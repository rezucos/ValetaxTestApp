using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ValetaxTest.EventBusKafka;

public interface IKafkaProducer
{
    Handle Handle { get; }

    Task<DeliveryResult<string, string>> ProduceAsync(string topic, Message<string, string> message, CancellationToken cancellationToken);
}

/// <summary>The one Kafka producer of the process: sends outbox messages and moves failed messages to .dlt topics.</summary>
internal sealed class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(IOptions<KafkaOptions> options, ILogger<KafkaProducer> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            ClientId = $"{options.Value.ClientId}-producer-{Environment.MachineName}",
            // The broker drops duplicates of retried sends and confirms a message only when all replicas have it;
            // the order within a partition is kept.
            EnableIdempotence = true,
            Acks = Acks.All,
            LingerMs = 5,
            MessageTimeoutMs = 30_000,
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, error) => logger.Log(
                error.IsFatal ? LogLevel.Critical : LogLevel.Warning,
                "Kafka producer error: {Reason} ({Code})", error.Reason, error.Code))
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
            .Build();
    }

    public Handle Handle => _producer.Handle;

    public Task<DeliveryResult<string, string>> ProduceAsync(string topic, Message<string, string> message, CancellationToken cancellationToken) =>
        _producer.ProduceAsync(topic, message, cancellationToken);

    public void Dispose()
    {
        try
        {
            // Deliver whatever is still buffered before the process exits.
            _producer.Flush(TimeSpan.FromSeconds(10));
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Kafka producer was not flushed on shutdown");
        }

        _producer.Dispose();
    }
}
