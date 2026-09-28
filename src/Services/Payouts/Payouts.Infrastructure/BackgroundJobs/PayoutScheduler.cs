using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ValetaxTest.Payouts.Application;
using ValetaxTest.Payouts.Application.Sagas;

namespace ValetaxTest.Payouts.Infrastructure.BackgroundJobs;

/// <summary>Pays out accrued commissions on a timer by calling <see cref="IPayoutService.RunAsync"/>.</summary>
public sealed class PayoutScheduler(
    IServiceScopeFactory scopeFactory,
    IOptions<PayoutOptions> options,
    TimeProvider timeProvider,
    ILogger<PayoutScheduler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.SchedulerEnabled)
        {
            logger.LogInformation("Payout scheduler is disabled");
            return;
        }

        var period = options.Value.Interval;
        logger.LogInformation("Payout scheduler runs every {Period}", period);
        using var timer = new PeriodicTimer(period, timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                using var activity = PayoutsTelemetry.Source.StartActivity("payouts.scheduled-run");
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<IPayoutService>().RunAsync(stoppingToken);
                }
                catch (Exception exception)
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }

                    activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
                    logger.LogError(exception, "Payout run failed, the next run is in {Period}", period);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutdown: an interrupted run is rolled back and done again after the next start.
        }
    }
}
