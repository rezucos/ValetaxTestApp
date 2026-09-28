using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ValetaxTest.MessageLogEF;

namespace ValetaxTest.EventBusKafka;

/// <summary>
/// Sends saved outbox messages to Kafka and deletes them. Several replicas can run it at once: each one
/// locks its rows with <c>SKIP LOCKED</c>. If the process crashes after sending but before deleting,
/// the message is sent again and consumers skip the duplicate.
/// </summary>
internal sealed class OutboxRelay<TContext>(
    IServiceScopeFactory scopeFactory,
    IKafkaProducer producer,
    IOptions<OutboxOptions> options,
    MessagingMetrics metrics,
    ILogger<OutboxRelay<TContext>> logger) : BackgroundService
    where TContext : DbContext
{
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        logger.LogInformation("Outbox relay of {DbContext} started", typeof(TContext).Name);

        var failures = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan delay;
            try
            {
                // Not tied to the stopping token: a batch that has started is finished even during shutdown.
                var (published, failed) = await PublishBatchAsync();
                failures = failed > 0 ? failures + 1 : 0;
                delay = failed > 0 ? TimeSpan.FromMilliseconds(Math.Min(500 * Math.Pow(2, failures - 1), MaxBackoff.TotalMilliseconds))
                    : published == options.Value.BatchSize ? TimeSpan.Zero
                    : options.Value.PollingInterval;
            }
            catch (Exception exception)
            {
                failures++;
                delay = TimeSpan.FromMilliseconds(Math.Min(500 * Math.Pow(2, failures - 1), MaxBackoff.TotalMilliseconds));
                logger.LogError(exception, "Outbox relay of {DbContext} failed, retrying in {Delay}", typeof(TContext).Name, delay);
            }

            if (delay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        logger.LogInformation("Outbox relay of {DbContext} stopped", typeof(TContext).Name);
    }

    private async Task<(int Published, int Failed)> PublishBatchAsync()
    {
        using var timeout = new CancellationTokenSource(options.Value.BatchTimeout);
        var cancellationToken = timeout.Token;

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var batchSize = options.Value.BatchSize;
        var messages = await dbContext.Set<OutboxMessage>()
            .FromSql($"SELECT * FROM outbox_messages ORDER BY id LIMIT {batchSize} FOR UPDATE SKIP LOCKED")
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            return (0, 0);
        }

        // The whole batch is sent at once; Kafka keeps the order within a partition.
        var deliveries = messages.Select(message => PublishAsync(message, cancellationToken)).ToList();
        var results = await Task.WhenAll(deliveries);

        var published = 0;
        for (var i = 0; i < messages.Count; i++)
        {
            if (results[i])
            {
                dbContext.Remove(messages[i]);
                published++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (published, messages.Count - published);
    }

    private async Task<bool> PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var storedContext = message.TraceContext is null
            ? null
            : JsonSerializer.Deserialize<Dictionary<string, string>>(message.TraceContext);

        using var activity = KafkaTelemetry.StartPublish(
            message.Topic, message.Type, message.MessageId, TraceContext.Extract(storedContext));

        var headers = new Headers()
            .AddString(MessageHeaders.MessageId, message.MessageId.ToString())
            .AddString(MessageHeaders.MessageType, message.Type)
            .AddString(MessageHeaders.OccurredAt, message.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
        if (message.CorrelationId is not null)
        {
            headers.AddString(MessageHeaders.CorrelationId, message.CorrelationId);
        }

        KafkaTelemetry.InjectContext(activity, storedContext, headers);

        try
        {
            var delivery = await producer.ProduceAsync(
                message.Topic,
                new Message<string, string> { Key = message.Key, Value = message.Payload, Headers = headers },
                cancellationToken);

            activity?.SetTag("messaging.destination.partition.id", delivery.Partition.Value.ToString(CultureInfo.InvariantCulture));
            metrics.Published(message.Topic, message.Type);
            return true;
        }
        catch (KafkaException exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Error.Reason);
            metrics.PublishFailed(message.Topic, message.Type);
            message.Attempts++;
            message.LastError = exception.Error.Reason.Length > 2000 ? exception.Error.Reason[..2000] : exception.Error.Reason;
            logger.LogWarning(exception, "Outbox message {MessageId} ({MessageType}) was not published to {Topic} (attempt {Attempt})",
                message.MessageId, message.Type, message.Topic, message.Attempts);
            return false;
        }
    }
}
