using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using ValetaxTest.EventBusKafka;
using ValetaxTest.PersistencePostgres;
using ValetaxTest.ServiceDefaults;
using ValetaxTest.Wallets.Application;
using ValetaxTest.Wallets.Application.Abstractions;
using ValetaxTest.Wallets.Application.LinkEvents;
using ValetaxTest.Wallets.Application.Payouts;
using ValetaxTest.Wallets.Application.Payouts.Messages;
using ValetaxTest.Wallets.Application.Wallets;
using ValetaxTest.Wallets.Domain.Wallets;
using ValetaxTest.Wallets.Infrastructure.Persistence;
using ValetaxTest.Wallets.Infrastructure.Persistence.Queries;
using ValetaxTest.Wallets.Infrastructure.Persistence.Repositories;

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
    document.Info.Title = "Wallets API";
    // No server list: the document is opened through the gateway, so Scalar sends requests to the gateway.
    document.Servers = [];
    return Task.CompletedTask;
}));

// Business logic.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<WalletRules>();
builder.Services.AddSingleton<WalletsMetrics>();
builder.Services.AddScoped<IWalletService, WalletService>();

// Database and Kafka.
builder.Services.AddPostgresPersistence<WalletsDbContext>(builder.Configuration);
builder.Services.AddKafkaMessaging(builder.Configuration);
builder.Services.AddOutbox<WalletsDbContext>(builder.Configuration);
builder.Services.AddKafkaConsumer<WalletsDbContext>("wallets", consumer => consumer
    .Handle<UserRegisteredLinkEvent, UserRegisteredLinkEventHandler>(Topics.UserRegistered)
    .Handle<CreditWallet, PayoutCommandsHandler>(Topics.WalletPayoutCommands));
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<IWalletPayoutRepository, WalletPayoutRepository>();
builder.Services.AddScoped<IWalletReader, WalletReader>();

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

await app.MigrateDatabaseAsync<WalletsDbContext>();
await app.RunAsync();
