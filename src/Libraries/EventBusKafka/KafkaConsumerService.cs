using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ValetaxTest.EventBus;
using ValetaxTest.EventBus.Abstractions;
using ValetaxTest.ServiceDefaults;

namespace ValetaxTest.EventBusKafka;

/// <summary>
/// Reads the topics of one consumer group and passes messages to their handlers. The offset is saved only
/// after a message is handled, so nothing is lost. A message that fails because a database or a service is
/// down is read again later; a message that can never be handled goes to the "&lt;topic&gt;.dlt" topic.
/// </summary>
internal sealed class KafkaConsumerService<TContext>(
    ConsumerRegistration registration,
    IServiceScopeFactory scopeFactory,
    IKafkaProducer producer,
    IOptions<KafkaOptions> kafkaOptions,
    MessagingMetrics metrics,
    TimeProvider timeProvider,
    ILogger<KafkaConsumerService<TContext>> logger) : BackgroundService
    where TContext : DbContext
{
    private const int MaxAttemptsInProcess = 3;
    private static readonly TimeSpan MaxRedeliveryDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan HandlerTimeout = TimeSpan.FromSeconds(20);

    private enum Outcome
    {
        Processed,
        Duplicate,
        DeadLettered,
        RetryLater,
        Aborted,
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Consume() blocks the calling thread: yield first so that host startup is not held up.
        await Task.Yield();

        using var consumer = BuildConsumer();
        consumer.Subscribe(registration.Topics);
        logger.LogInformation("Kafka consumer {Consumer} subscribed to {Topics}",
            registration.Name, string.Join(", ", registration.Topics));

        var consecutiveRedeliveries = 0;
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result;
                try
                {
                    result = consumer.Consume(stoppingToken);
                }
                catch (ConsumeException exception)
                {
                    logger.LogWarning(exception, "Kafka consumer {Consumer} failed to fetch messages: {Reason}",
                        registration.Name, exception.Error.Reason);
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                if (result?.Message is null)
                {
                    continue;
                }

                var startedAt = timeProvider.GetTimestamp();
                var messageType = result.Message.Headers.GetString(MessageHeaders.MessageType);
                Outcome outcome;
                using (var activity = KafkaTelemetry.StartProcess(
                           result, registration.Name, messageType, KafkaTelemetry.ExtractContext(result.Message.Headers)))
                {
                    outcome = await DispatchAsync(result, messageType, stoppingToken);
                    if (outcome is Outcome.RetryLater or Outcome.DeadLettered)
                    {
                        activity?.SetStatus(ActivityStatusCode.Error, outcome.ToString());
                    }
                }

                metrics.Consumed(registration.Name, result.Topic, messageType ?? "unknown", outcome.ToString(),
                    timeProvider.GetElapsedTime(startedAt));

                switch (outcome)
                {
                    case Outcome.Aborted:
                        // Stopped in the middle of retries: the offset is not saved, the message is read again after restart.
                        return;

                    case Outcome.RetryLater:
                        consecutiveRedeliveries++;
                        try
                        {
                            consumer.Seek(result.TopicPartitionOffset);
                        }
                        catch (KafkaException exception)
                        {
                            logger.LogWarning(exception, "Could not rewind {Topic}[{Partition}] to {Offset}",
                                result.Topic, result.Partition.Value, result.Offset.Value);
                        }

                        var delay = Math.Min(Math.Pow(2, consecutiveRedeliveries - 1), MaxRedeliveryDelay.TotalSeconds);
                        await Task.Delay(TimeSpan.FromSeconds(delay), stoppingToken);
                        break;

                    default:
                        consecutiveRedeliveries = 0;
                        try
                        {
                            consumer.StoreOffset(result);
                        }
                        catch (KafkaException exception)
                        {
                            // The partition moved to another replica while the message was handled. That replica
                            // reads the message again and skips it: it is already in the inbox.
                            logger.LogWarning(exception, "Offset of {Topic}[{Partition}]@{Offset} was not stored",
                                result.Topic, result.Partition.Value, result.Offset.Value);
                        }

                        break;
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        finally
        {
            try
            {
                // Saves the offsets and leaves the group at once, so other replicas take over its partitions without waiting.
                consumer.Close();
                logger.LogInformation("Kafka consumer {Consumer} closed", registration.Name);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Kafka consumer {Consumer} was not closed cleanly", registration.Name);
            }
        }
    }

    private async Task<Outcome> DispatchAsync(ConsumeResult<string, string> result, string? messageType, CancellationToken stoppingToken)
    {
        if (messageType is null || !registration.Handlers.TryGetValue(messageType, out var descriptor))
        {
            return await DeadLetterAsync(result, $"No handler for message type '{messageType ?? "<missing>"}'", exception: null);
        }

        object message;
        try
        {
            message = MessageSerializer.Deserialize(result.Message.Value, descriptor.MessageType);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or ArgumentException)
        {
            return await DeadLetterAsync(result, "Payload cannot be deserialized", exception);
        }

        var messageId = Guid.TryParse(result.Message.Headers.GetString(MessageHeaders.MessageId), out var parsedId)
            ? parsedId
            // A message without an id (e.g. written by hand) gets an id made from its position in the topic.
            : new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(
                $"{result.Topic}/{result.Partition.Value}/{result.Offset.Value}")).AsSpan(0, 16));

        var context = new MessageContext(
            MessageId: messageId,
            MessageType: messageType,
            Topic: result.Topic,
            Key: result.Message.Key,
            CorrelationId: result.Message.Headers.GetString(MessageHeaders.CorrelationId),
            Partition: result.Partition.Value,
            Offset: result.Offset.Value);

        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["MessageId"] = context.MessageId,
            ["MessageType"] = context.MessageType,
            ["CorrelationId"] = context.CorrelationId,
        });

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await HandleInTransactionAsync(descriptor, message, context) ? Outcome.Processed : Outcome.Duplicate;
            }
            catch (Exception exception)
            {
                if (attempt < MaxAttemptsInProcess)
                {
                    logger.LogWarning(exception, "Attempt {Attempt} to handle {MessageType} from {Topic} failed, retrying",
                        attempt, context.MessageType, context.Topic);
                    try
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(200 * Math.Pow(4, attempt - 1)), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        return Outcome.Aborted;
                    }

                    continue;
                }

                if (TransientErrorDetector.IsTransient(exception))
                {
                    logger.LogWarning(exception,
                        "{MessageType} from {Topic} cannot be handled while a dependency is unavailable, it will be redelivered",
                        context.MessageType, context.Topic);
                    return Outcome.RetryLater;
                }

                logger.LogError(exception, "{MessageType} from {Topic} failed {Attempts} times",
                    context.MessageType, context.Topic, attempt);
                return await DeadLetterAsync(result, "Handler failed", exception);
            }
        }
    }

    /// <returns><c>false</c> when the message was already processed by this consumer.</returns>
    private async Task<bool> HandleInTransactionAsync(HandlerDescriptor descriptor, object message, MessageContext context)
    {
        // Not tied to the stopping token: a message that has started is finished even during shutdown.
        using var timeout = new CancellationTokenSource(HandlerTimeout);
        var cancellationToken = timeout.Token;

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var processedAt = timeProvider.GetUtcNow();
        var inserted = await dbContext.Database.ExecuteSqlAsync(
            $"""
             INSERT INTO inbox_messages (message_id, consumer, message_type, processed_at)
             VALUES ({context.MessageId}, {registration.Name}, {context.MessageType}, {processedAt})
             ON CONFLICT DO NOTHING
             """,
            cancellationToken);

        if (inserted == 0)
        {
            logger.LogInformation("{MessageType} {MessageId} was already processed by {Consumer}, skipping",
                context.MessageType, context.MessageId, registration.Name);
            return false;
        }

        await descriptor.Handle(scope.ServiceProvider, message, context, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<Outcome> DeadLetterAsync(ConsumeResult<string, string> result, string reason, Exception? exception)
    {
        var deadLetterTopic = result.Topic + ".dlt";

        var headers = new Headers();
        if (result.Message.Headers is not null)
        {
            foreach (var header in result.Message.Headers)
            {
                headers.Add(header.Key, header.GetValueBytes());
            }
        }

        headers
            .AddString(MessageHeaders.DeadLetterReason, reason)
            .AddString(MessageHeaders.DeadLetterOriginalTopic, result.Topic)
            .AddString(MessageHeaders.DeadLetterOriginalPartition, result.Partition.Value.ToString(CultureInfo.InvariantCulture))
            .AddString(MessageHeaders.DeadLetterOriginalOffset, result.Offset.Value.ToString(CultureInfo.InvariantCulture))
            .AddString(MessageHeaders.DeadLetterConsumer, registration.Name)
            .AddString(MessageHeaders.DeadLetterFailedAt, timeProvider.GetUtcNow().ToString("O", CultureInfo.InvariantCulture));

        if (exception is not null)
        {
            var error = $"{exception.GetType().Name}: {exception.Message}";
            headers.AddString(MessageHeaders.DeadLetterError, error.Length > 1000 ? error[..1000] : error);
        }

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await producer.ProduceAsync(
                deadLetterTopic,
                new Message<string, string> { Key = result.Message.Key, Value = result.Message.Value, Headers = headers },
                timeout.Token);

            metrics.DeadLettered(registration.Name, result.Topic);
            logger.LogError(exception, "Message {Topic}[{Partition}]@{Offset} moved to {DeadLetterTopic}: {Reason}",
                result.Topic, result.Partition.Value, result.Offset.Value, deadLetterTopic, reason);
            return Outcome.DeadLettered;
        }
        catch (Exception publishException)
        {
            logger.LogError(publishException,
                "Message {Topic}[{Partition}]@{Offset} could not be moved to {DeadLetterTopic}, it will be redelivered",
                result.Topic, result.Partition.Value, result.Offset.Value, deadLetterTopic);
            return Outcome.RetryLater;
        }
    }

    private IConsumer<string, string> BuildConsumer()
    {
        var options = kafkaOptions.Value;
        var config = new ConsumerConfig
        {
            BootstrapServers = options.BootstrapServers,
            GroupId = registration.Name,
            ClientId = $"{options.ClientId}-{registration.Name}-{Environment.MachineName}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            // We mark the offset ourselves after handling a message; marked offsets are committed every second.
            EnableAutoCommit = true,
            AutoCommitIntervalMs = 1000,
            EnableAutoOffsetStore = false,
            // Adding or removing a replica does not pause the other consumers of the group.
            PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky,
            SessionTimeoutMs = 30_000,
            MaxPollIntervalMs = 300_000,
        };

        return new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, error) => logger.Log(
                error.IsFatal ? LogLevel.Critical : LogLevel.Warning,
                "Kafka consumer {Consumer} error: {Reason} ({Code})", registration.Name, error.Reason, error.Code))
            // librdkafka's own log: only warnings and errors are interesting outside of debugging.
            .SetLogHandler((_, message) => logger.Log(
                message.Level switch
                {
                    SyslogLevel.Emergency or SyslogLevel.Alert or SyslogLevel.Critical => LogLevel.Critical,
                    SyslogLevel.Error => LogLevel.Error,
                    SyslogLevel.Warning => LogLevel.Warning,
                    _ => LogLevel.Debug,
                },
                "librdkafka [{Facility}] {Message}", message.Facility, message.Message))
            .SetPartitionsAssignedHandler((_, partitions) => logger.LogInformation(
                "Kafka consumer {Consumer} assigned {Partitions}", registration.Name, Describe(partitions)))
            .SetPartitionsRevokedHandler((_, partitions) => logger.LogInformation(
                "Kafka consumer {Consumer} revoked {Partitions}", registration.Name, Describe(partitions.Select(p => p.TopicPartition))))
            .SetPartitionsLostHandler((_, partitions) => logger.LogWarning(
                "Kafka consumer {Consumer} lost {Partitions}", registration.Name, Describe(partitions.Select(p => p.TopicPartition))))
            .Build();
    }

    private static string Describe(IEnumerable<TopicPartition> partitions) =>
        string.Join(", ", partitions.Select(partition => $"{partition.Topic}[{partition.Partition.Value}]"));
}
