using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ValetaxTest.ServiceDefaults;

/// <summary>Writes the health report as JSON: overall status and each check with its duration and error.</summary>
public static class HealthResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        var payload = new
        {
            status = report.Status.ToString(),
            durationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 1),
                description = entry.Value.Description,
                error = entry.Value.Exception?.Message,
            }),
        };

        return context.Response.WriteAsJsonAsync(payload, context.RequestAborted);
    }
}
