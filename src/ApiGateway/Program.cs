using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Rewrite;
using Scalar.AspNetCore;
using ValetaxTest.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .ConfigureHttpClient((_, handler) =>
        // Connections are reopened every 30 seconds, so new replicas (docker compose --scale) start getting requests.
        handler.PooledConnectionLifetime = TimeSpan.FromSeconds(30));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// The gateway has no database or Kafka: it is ready while it runs and is not shutting down.
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

// One Scalar page for all services; their OpenAPI documents come through the gateway as /openapi/{service}.json.
var documents = builder.Configuration.GetSection("ApiDocuments").Get<ApiDocument[]>() ?? [];
app.MapScalarApiReference("/scalar", options =>
{
    options.WithTitle("ValetaxTest commissions");
    foreach (var document in documents)
    {
        options.AddDocument(document.Name, document.Title);
    }
});
app.UseRewriter(new RewriteOptions().AddRedirect("^$", "scalar"));
app.MapReverseProxy();

await app.RunAsync();

internal sealed class ApiDocument
{
    public string Name { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
}
