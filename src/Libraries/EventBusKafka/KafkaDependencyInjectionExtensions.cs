using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;
using ValetaxTest.EventBus.Abstractions;
using ValetaxTest.MessageLogEF;
using ValetaxTest.ServiceDefaults;

namespace ValetaxTest.EventBusKafka;

public static class KafkaDependencyInjectionExtensions
{
    /// <summary>Registers the Kafka producer, the Kafka readiness check and messaging telemetry.</summary>
    public static IServiceCollection AddKafkaMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<KafkaOptions>()
            .Bind(configuration.GetSection(KafkaOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.BootstrapServers), "Kafka:BootstrapServers must be set")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ClientId), "Kafka:ClientId must be set")
            .ValidateOnStart();
        services.TryAddSingleton<IKafkaProducer, KafkaProducer>();
        services.TryAddSingleton<MessagingMetrics>();

        services.AddHealthChecks()
            .AddCheck<KafkaHealthCheck>("kafka", tags: [ServiceDefaultsExtensions.ReadyTag]);

        services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddSource(KafkaTelemetry.SourceName));

        return services;
    }

    /// <summary>Registers the outbox stored in <typeparamref name="TContext"/> and the background job that sends it.</summary>
    public static IServiceCollection AddOutbox<TContext>(this IServiceCollection services, IConfiguration configuration)
        where TContext : DbContext
    {
        services.AddOptions<OutboxOptions>()
            .Bind(configuration.GetSection(OutboxOptions.SectionName))
            .Validate(options => options.PollingInterval > TimeSpan.Zero, "Outbox:PollingInterval must be positive")
            .Validate(options => options.BatchSize >= 1, "Outbox:BatchSize must be positive")
            .Validate(options => options.BatchTimeout > TimeSpan.Zero, "Outbox:BatchTimeout must be positive")
            .ValidateOnStart();
        services.AddScoped<IOutbox, EfOutbox<TContext>>();
        services.AddHostedService<OutboxRelay<TContext>>();

        return services;
    }

    /// <summary>Registers consumer group <paramref name="name"/>; each message is handled in a transaction of <typeparamref name="TContext"/>.</summary>
    public static IServiceCollection AddKafkaConsumer<TContext>(
        this IServiceCollection services,
        string name,
        Action<KafkaConsumerBuilder> configure)
        where TContext : DbContext
    {
        var registration = new ConsumerRegistration(name);
        configure(new KafkaConsumerBuilder(services, registration));

        services.AddSingleton<IHostedService>(provider =>
            ActivatorUtilities.CreateInstance<KafkaConsumerService<TContext>>(provider, registration));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, InboxCleanupService<TContext>>());

        return services;
    }
}
