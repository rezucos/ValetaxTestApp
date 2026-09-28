using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ValetaxTest.Payouts.Application;
using ValetaxTest.Payouts.Application.Sagas;

namespace ValetaxTest.Payouts.Infrastructure.BackgroundJobs;

/// <summary>Calls <see cref="IPayoutService.RetryTimedOutStepsAsync"/> on a timer to send again the commands that got no reply.</summary>
public sealed class SagaTimeoutMonitor(
    IServiceScopeFactory scopeFactory,
    IOptions<PayoutOptions> options,
    TimeProvider timeProvider,
    ILogger<SagaTimeoutMonitor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var period = options.Value.TimeoutCheckInterval;
        logger.LogInformation("Saga timeout monitor runs every {Period}", period);
        using var timer = new PeriodicTimer(period, timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                using var activity = PayoutsTelemetry.Source.StartActivity("payouts.timeout-monitor");
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<IPayoutService>().RetryTimedOutStepsAsync(stoppingToken);
                }
                catch (Exception exception)
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }

                    activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
                    logger.LogError(exception, "Saga timeout check failed, the next check is in {Period}", period);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutdown: an interrupted check is rolled back and done again after the next start.
        }
    }
}
