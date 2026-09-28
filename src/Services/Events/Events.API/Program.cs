using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using ValetaxTest.EventBusKafka;
using ValetaxTest.Events.Application;
using ValetaxTest.Events.Application.Abstractions;
using ValetaxTest.Events.Application.LinkEvents;
using ValetaxTest.Events.Application.ProfitEvents;
using ValetaxTest.Events.Domain.ProfitEvents;
using ValetaxTest.Events.Infrastructure.Partners;
using ValetaxTest.Events.Infrastructure.Persistence;
using ValetaxTest.Events.Infrastructure.Persistence.Queries;
using ValetaxTest.Events.Infrastructure.Persistence.Repositories;
using ValetaxTest.Partners.API.Grpc;
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
    document.Info.Title = "Events API";
    // No server list: the document is opened through the gateway, so Scalar sends requests to the gateway.
    document.Servers = [];
    return Task.CompletedTask;
}));

// Business logic and request validators.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ProfitEventService>();
builder.Services.AddSingleton<EventsMetrics>();
builder.Services.AddSingleton<IValidator<RegisterProfitEventRequest>, RegisterProfitEventRequestValidator>();
builder.Services.AddScoped<IEventService, EventService>();

// Database, Kafka and the Partners gRPC client.
builder.Services.AddPostgresPersistence<EventsDbContext>(builder.Configuration);
builder.Services.AddKafkaMessaging(builder.Configuration);
builder.Services.AddOutbox<EventsDbContext>(builder.Configuration);
// Commissions of events and their payments, as reported by the Commissions service.
builder.Services.AddKafkaConsumer<EventsDbContext>("events-read-model", consumer => consumer
    .Handle<CommissionsCalculatedLinkEvent, CommissionsCalculatedLinkEventHandler>(Topics.CommissionsCalculated)
    .Handle<CommissionsPaidLinkEvent, CommissionsPaidLinkEventHandler>(Topics.CommissionsPaid));
builder.Services.AddMemoryCache();
builder.Services.AddGrpcServiceClient(UserDirectoryGrpcAdapter.ServiceName, builder.Configuration,
    channel => new PartnersDirectory.PartnersDirectoryClient(channel));
builder.Services.AddSingleton<IUserDirectory, UserDirectoryGrpcAdapter>();
builder.Services.AddScoped<IProfitEventRepository, ProfitEventRepository>();
builder.Services.AddScoped<ICommissionPaymentRepository, CommissionPaymentRepository>();
builder.Services.AddScoped<IProfitEventReader, ProfitEventReader>();

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

await app.MigrateDatabaseAsync<EventsDbContext>();
await app.RunAsync();
