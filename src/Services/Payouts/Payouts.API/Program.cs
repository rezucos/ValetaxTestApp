using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using ValetaxTest.Commissions.API.Grpc;
using ValetaxTest.EventBusKafka;
using ValetaxTest.Payouts.Application;
using ValetaxTest.Payouts.Application.Abstractions;
using ValetaxTest.Payouts.Application.Sagas;
using ValetaxTest.Payouts.Application.Sagas.Messages;
using ValetaxTest.Payouts.Domain.Sagas;
using ValetaxTest.Payouts.Infrastructure.BackgroundJobs;
using ValetaxTest.Payouts.Infrastructure.Commissions;
using ValetaxTest.Payouts.Infrastructure.Persistence;
using ValetaxTest.Payouts.Infrastructure.Persistence.Queries;
using ValetaxTest.Payouts.Infrastructure.Persistence.Repositories;
using ValetaxTest.PersistencePostgres;
using ValetaxTest.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// HTTP API.
// Requests are checked by FluentValidation in the services, so MVC must not add its own [Required] checks.
builder.Services.AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(new NormalizedDecimalJsonConverter());
    });
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Info.Title = "Payouts API (saga orchestrator)";
    // No server list: the document is opened through the gateway, so Scalar sends requests to the gateway.
    document.Servers = [];
    return Task.CompletedTask;
}));

// Business logic.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(provider =>
{
    var options = provider.GetRequiredService<IOptions<PayoutOptions>>().Value;
    return new PayoutSagaStateMachine(new PayoutSagaTiming(options.StepTimeout, options.MaxRetryDelay));
});
builder.Services.AddSingleton<PayoutsMetrics>();
builder.Services.AddScoped<PayoutCommandSender>();
builder.Services.AddScoped<IPayoutService, PayoutService>();

// Database, Kafka, the Commissions gRPC client and background jobs.
builder.Services.AddOptions<PayoutOptions>()
    .Bind(builder.Configuration.GetSection(PayoutOptions.SectionName))
    .Validate(options => options.Interval > TimeSpan.Zero, "Payouts:Interval must be positive")
    .Validate(options => options.StepTimeout > TimeSpan.Zero, "Payouts:StepTimeout must be positive")
    .Validate(options => options.MaxRetryDelay >= options.StepTimeout, "Payouts:MaxRetryDelay must not be less than StepTimeout")
    .Validate(options => options.TimeoutCheckInterval > TimeSpan.Zero, "Payouts:TimeoutCheckInterval must be positive")
    .Validate(options => options.MaxItemsPerPayout is >= 1 and <= 5000, "Payouts:MaxItemsPerPayout must be within 1..5000")
    .Validate(options => options.MaxPartnersPerRun >= 1, "Payouts:MaxPartnersPerRun must be positive")
    .ValidateOnStart();
builder.Services.AddPostgresPersistence<PayoutsDbContext>(builder.Configuration);
builder.Services.AddKafkaMessaging(builder.Configuration);
builder.Services.AddOutbox<PayoutsDbContext>(builder.Configuration);
builder.Services.AddKafkaConsumer<PayoutsDbContext>("payouts-orchestrator", consumer => consumer
    .Handle<CommissionsReserved, PayoutSagaRepliesHandler>(Topics.PayoutSagaReplies)
    .Handle<WalletCredited, PayoutSagaRepliesHandler>(Topics.PayoutSagaReplies)
    .Handle<WalletCreditRejected, PayoutSagaRepliesHandler>(Topics.PayoutSagaReplies)
    .Handle<CommissionsPaymentConfirmed, PayoutSagaRepliesHandler>(Topics.PayoutSagaReplies)
    .Handle<CommissionsReleased, PayoutSagaRepliesHandler>(Topics.PayoutSagaReplies));
builder.Services.AddGrpcServiceClient(CommissionsLedgerGrpcAdapter.ServiceName, builder.Configuration,
    channel => new CommissionsLedger.CommissionsLedgerClient(channel));
builder.Services.AddSingleton<ICommissionsLedger, CommissionsLedgerGrpcAdapter>();
builder.Services.AddScoped<IPayoutSagaRepository, PayoutSagaRepository>();
builder.Services.AddScoped<IPayoutSagaReader, PayoutSagaReader>();
builder.Services.AddHostedService<PayoutScheduler>();
builder.Services.AddHostedService<SagaTimeoutMonitor>();
builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddSource(PayoutsTelemetry.SourceName));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Liveness does not check the database and Kafka: when they are down, containers should wait, not restart.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthResponseWriter.WriteAsync,
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains(ServiceDefaultsExtensions.ReadyTag),
    ResponseWriter = HealthResponseWriter.WriteAsync,
});
app.MapOpenApi();
app.MapControllers();

await app.MigrateDatabaseAsync<PayoutsDbContext>();
await app.RunAsync();
