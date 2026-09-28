using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace ValetaxTest.ServiceDefaults;

/// <summary>
/// Reports "not ready" as soon as shutdown starts, so no new traffic is sent to the instance
/// while it finishes the current requests and messages.
/// </summary>
internal sealed class ShutdownHealthCheck(IHostApplicationLifetime lifetime) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(lifetime.ApplicationStopping.IsCancellationRequested
            ? HealthCheckResult.Unhealthy("Application is shutting down")
            : HealthCheckResult.Healthy());
}
