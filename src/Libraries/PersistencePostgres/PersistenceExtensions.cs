using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Polly;
using Polly.Retry;
using ValetaxTest.Core.Persistence;
using ValetaxTest.ServiceDefaults;

namespace ValetaxTest.PersistencePostgres;

public static class PersistenceExtensions
{
    public const string ConnectionStringName = "Database";

    /// <summary>
    /// Registers the service's DbContext on PostgreSQL (snake_case names), the unit of work, advisory locks,
    /// the database readiness check and Npgsql telemetry.
    /// </summary>
    public static IServiceCollection AddPostgresPersistence<TContext>(this IServiceCollection services, IConfiguration configuration)
        where TContext : DbContext
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        services.AddDbContext<TContext>(options => options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history"))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork, EfUnitOfWork<TContext>>();
        services.AddScoped<ITransactionalLock, PostgresTransactionalLock<TContext>>();

        services.AddHealthChecks()
            .AddDbContextCheck<TContext>("database", tags: [ServiceDefaultsExtensions.ReadyTag]);

        services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddNpgsql());
        services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddMeter("Npgsql"));

        return services;
    }

    /// <summary>
    /// Applies pending migrations on startup, retrying while the database is not reachable yet.
    /// Replicas starting at the same time are safe: EF Core locks the database while migrating.
    /// </summary>
    public static async Task MigrateDatabaseAsync<TContext>(this IHost host) where TContext : DbContext
    {
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("ValetaxTest.DatabaseMigration");
        var stopping = host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(TransientErrorDetector.IsTransient),
                MaxRetryAttempts = 10,
                Delay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromSeconds(10),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    logger.LogWarning(args.Outcome.Exception,
                        "Database is not available yet (attempt {Attempt}), retrying in {Delay}",
                        args.AttemptNumber + 1, args.RetryDelay);
                    return ValueTask.CompletedTask;
                },
            })
            .Build();

        await pipeline.ExecuteAsync(async token =>
        {
            await using var scope = host.Services.CreateAsyncScope();

            // On the first run EF Core queries the migrations history table before creating it;
            // this expected error is logged at Debug so that it does not look like a failure.
            var options = new DbContextOptionsBuilder<TContext>(scope.ServiceProvider.GetRequiredService<DbContextOptions<TContext>>())
                .ConfigureWarnings(warnings => warnings.Log((RelationalEventId.CommandError, LogLevel.Debug)))
                .Options;

            await using var dbContext = ActivatorUtilities.CreateInstance<TContext>(scope.ServiceProvider, options);
            await dbContext.Database.MigrateAsync(token);
        }, stopping);

        logger.LogInformation("Database schema of {DbContext} is up to date", typeof(TContext).Name);
    }
}
