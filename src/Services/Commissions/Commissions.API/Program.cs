using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using ValetaxTest.Commissions.API.Grpc;
using ValetaxTest.Commissions.Application;
using ValetaxTest.Commissions.Application.Abstractions;
using ValetaxTest.Commissions.Application.Commissions;
using ValetaxTest.Commissions.Application.LinkEvents;
using ValetaxTest.Commissions.Application.Payouts;
using ValetaxTest.Commissions.Application.Payouts.Messages;
using ValetaxTest.Commissions.Domain.Accrual;
using ValetaxTest.Commissions.Domain.Payouts;
using ValetaxTest.Commissions.Domain.Schemas;
using ValetaxTest.Commissions.Infrastructure.Partners;
using ValetaxTest.Commissions.Infrastructure.Persistence;
using ValetaxTest.Commissions.Infrastructure.Persistence.Queries;
using ValetaxTest.Commissions.Infrastructure.Persistence.Repositories;
using ValetaxTest.EventBusKafka;
using ValetaxTest.Partners.API.Grpc;
using ValetaxTest.PersistencePostgres;
using ValetaxTest.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// HTTP API and the internal gRPC server.
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
    document.Info.Title = "Commissions API";
    // No server list: the document is opened through the gateway, so Scalar sends requests to the gateway.
    document.Servers = [];
    return Task.CompletedTask;
}));
builder.Services.AddGrpc(options => options.Interceptors.Add<GrpcExceptionInterceptor>());

// Business logic and request validators.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ICommissionSchema, LinearCommissionSchema>();
builder.Services.AddSingleton<ICommissionSchema, FibonacciCommissionSchema>();
builder.Services.AddSingleton<CommissionSchemaResolver>();
builder.Services.AddSingleton<CommissionSchemaService>();
builder.Services.AddSingleton<CommissionCalculator>();
builder.Services.AddSingleton<CommissionAccrualService>();
builder.Services.AddSingleton<PayoutReservationService>();
builder.Services.AddSingleton<CommissionsMetrics>();
builder.Services.AddSingleton<IValidator<SwitchSchemaRequest>, SwitchSchemaRequestValidator>();
builder.Services.AddScoped<ICommissionService, CommissionService>();

// Database, Kafka and the Partners gRPC client.
builder.Services.AddOptions<CommissionOptions>()
    .Bind(builder.Configuration.GetSection(CommissionOptions.SectionName))
    .Validate(options => options.MaxLevels is >= 1 and <= FibonacciCommissionSchema.MaxLevel,
        $"Commissions:MaxLevels must be within 1..{FibonacciCommissionSchema.MaxLevel}")
    .ValidateOnStart();
builder.Services.AddPostgresPersistence<CommissionsDbContext>(builder.Configuration);
builder.Services.AddKafkaMessaging(builder.Configuration);
builder.Services.AddOutbox<CommissionsDbContext>(builder.Configuration);
// Two consumer groups, so that a flood of profit events does not delay payouts and vice versa.
builder.Services.AddKafkaConsumer<CommissionsDbContext>("commissions-accrual", consumer => consumer
    .Handle<ProfitEventRegisteredLinkEvent, ProfitEventRegisteredLinkEventHandler>(Topics.ProfitEventRegistered));
builder.Services.AddKafkaConsumer<CommissionsDbContext>("commissions-payouts", consumer => consumer
    .Handle<ReserveCommissions, PayoutCommandsHandler>(Topics.CommissionPayoutCommands)
    .Handle<ConfirmCommissionsPaid, PayoutCommandsHandler>(Topics.CommissionPayoutCommands)
    .Handle<ReleaseCommissions, PayoutCommandsHandler>(Topics.CommissionPayoutCommands));
builder.Services.AddGrpcServiceClient(PartnerHierarchyGrpcAdapter.ServiceName, builder.Configuration,
    channel => new PartnersDirectory.PartnersDirectoryClient(channel));
builder.Services.AddSingleton<IPartnerHierarchy, PartnerHierarchyGrpcAdapter>();
builder.Services.AddScoped<ICommissionRepository, CommissionRepository>();
builder.Services.AddScoped<IProcessedEventRepository, ProcessedEventRepository>();
builder.Services.AddScoped<ICommissionSettingsRepository, CommissionSettingsRepository>();
builder.Services.AddScoped<IPayoutReservationRepository, PayoutReservationRepository>();
builder.Services.AddScoped<ICommissionReader, CommissionReader>();

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
app.MapGrpcService<CommissionsLedgerGrpcService>();

await app.MigrateDatabaseAsync<CommissionsDbContext>();
await app.RunAsync();
