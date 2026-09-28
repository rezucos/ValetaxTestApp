using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ValetaxTest.ServiceDefaults;

/// <summary>
/// Host settings shared by all services: shutdown timeout, OpenTelemetry (traces, metrics, logs)
/// and a readiness check that fails once shutdown has started.
/// </summary>
public static class ServiceDefaultsExtensions
{
    /// <summary>Health checks with this tag are included in <c>/health/ready</c>.</summary>
    public const string ReadyTag = "ready";

    public static WebApplicationBuilder AddServiceDefaults(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<HostOptions>(options =>
        {
            // How long Kafka consumers and outbox relays may finish their current work after SIGTERM.
            options.ShutdownTimeout = builder.Configuration.GetValue("Service:ShutdownTimeout", TimeSpan.FromSeconds(30));
        });

        builder.Logging.Configure(options =>
            options.ActivityTrackingOptions = ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId);
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddHealthChecks()
            .AddCheck<ShutdownHealthCheck>("shutdown", tags: [ReadyTag]);

        var serviceName = builder.Configuration["Service:Name"] ?? builder.Environment.ApplicationName;
        var openTelemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName,
                serviceNamespace: "valetaxtest",
                serviceInstanceId: Environment.MachineName))
            .WithTracing(tracing => tracing
                .SetSampler(new ParentBasedSampler(new SkipOrphanClientSpansSampler()))
                // Probes and API documentation are not traced.
                .AddAspNetCoreInstrumentation(options => options.Filter = context =>
                    !context.Request.Path.StartsWithSegments("/health") &&
                    !context.Request.Path.StartsWithSegments("/openapi") &&
                    !context.Request.Path.StartsWithSegments("/scalar"))
                .AddHttpClientInstrumentation())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("ValetaxTest.*")
                .AddMeter("Polly"));

        // Telemetry is sent only when a collector is configured (the Aspire Dashboard in docker-compose).
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            openTelemetry.UseOtlpExporter();
        }

        return builder;
    }
}
