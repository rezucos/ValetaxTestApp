using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using ValetaxTest.EventBusKafka;
using ValetaxTest.Partners.API.Grpc;
using ValetaxTest.Partners.Application;
using ValetaxTest.Partners.Application.Abstractions;
using ValetaxTest.Partners.Application.Users;
using ValetaxTest.Partners.Domain.Users;
using ValetaxTest.Partners.Infrastructure.Persistence;
using ValetaxTest.Partners.Infrastructure.Persistence.Queries;
using ValetaxTest.Partners.Infrastructure.Persistence.Repositories;
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
    document.Info.Title = "Partners API";
    // No server list: the document is opened through the gateway, so Scalar sends requests to the gateway.
    document.Servers = [];
    return Task.CompletedTask;
}));
builder.Services.AddGrpc(options => options.Interceptors.Add<GrpcExceptionInterceptor>());

// Business logic and request validators.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<PartnerTreeService>();
builder.Services.AddSingleton<IValidator<RegisterUserRequest>, RegisterUserRequestValidator>();
builder.Services.AddSingleton<IValidator<AssignPartnerRequest>, AssignPartnerRequestValidator>();
builder.Services.AddScoped<IUserService, UserService>();

// Database and Kafka.
builder.Services.AddOptions<PartnerTreeOptions>()
    .Bind(builder.Configuration.GetSection(PartnerTreeOptions.SectionName))
    .Validate(options => options.MaxDepth is >= 1 and <= 100, "PartnerTree:MaxDepth must be within 1..100")
    .Validate(options => options.MaxDownlineSize >= 1, "PartnerTree:MaxDownlineSize must be positive")
    .ValidateOnStart();
builder.Services.AddPostgresPersistence<PartnersDbContext>(builder.Configuration);
builder.Services.AddKafkaMessaging(builder.Configuration);
builder.Services.AddOutbox<PartnersDbContext>(builder.Configuration);
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPartnerTreeReader, PartnerTreeReader>();

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
app.MapGrpcService<PartnersDirectoryGrpcService>();

await app.MigrateDatabaseAsync<PartnersDbContext>();
await app.RunAsync();
