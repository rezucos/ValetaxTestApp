using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ValetaxTest.MessageLogEF;

/// <summary>
/// Deletes inbox records older than Kafka keeps messages: once a message is gone from the topic
/// it cannot arrive again, so its record is no longer needed.
/// </summary>
public sealed class InboxCleanupService<TContext>(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<InboxCleanupService<TContext>> logger) : BackgroundService
    where TContext : DbContext
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);
    private static readonly TimeSpan Retention = TimeSpan.FromDays(8);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
                    var threshold = timeProvider.GetUtcNow() - Retention;
                    var deleted = await dbContext.Set<InboxMessage>()
                        .Where(message => message.ProcessedAt < threshold)
                        .ExecuteDeleteAsync(stoppingToken);

                    if (deleted > 0)
                    {
                        logger.LogInformation("Removed {Count} inbox records processed before {Threshold}", deleted, threshold);
                    }
                }
                catch (Exception exception)
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }

                    logger.LogError(exception, "Inbox cleanup failed, the next cleanup is in {Period}", Interval);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutdown: an interrupted cleanup is done again after the next start.
        }
    }
}
