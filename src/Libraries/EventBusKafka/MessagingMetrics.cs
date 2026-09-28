using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ValetaxTest.EventBusKafka;

public sealed class MessagingMetrics
{
    public const string MeterName = "ValetaxTest.Messaging";

    private readonly Counter<long> _consumed;
    private readonly Histogram<double> _processingDuration;
    private readonly Counter<long> _deadLettered;
    private readonly Counter<long> _published;
    private readonly Counter<long> _publishFailures;

    public MessagingMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _consumed = meter.CreateCounter<long>(
            "valetaxtest.messaging.consumed", "{message}", "Messages consumed from Kafka, by processing outcome");
        _processingDuration = meter.CreateHistogram<double>(
            "valetaxtest.messaging.process.duration", "s", "Time spent processing a consumed message");
        _deadLettered = meter.CreateCounter<long>(
            "valetaxtest.messaging.dead_lettered", "{message}", "Messages moved to a dead-letter topic");
        _published = meter.CreateCounter<long>(
            "valetaxtest.messaging.published", "{message}", "Outbox messages published to Kafka");
        _publishFailures = meter.CreateCounter<long>(
            "valetaxtest.messaging.publish.failures", "{message}", "Failed attempts to publish outbox messages");
    }

    public void Consumed(string consumer, string topic, string messageType, string outcome, TimeSpan duration)
    {
        var tags = new TagList
        {
            { "consumer", consumer },
            { "topic", topic },
            { "message_type", messageType },
            { "outcome", outcome },
        };
        _consumed.Add(1, tags);
        _processingDuration.Record(duration.TotalSeconds, tags);
    }

    public void DeadLettered(string consumer, string topic) =>
        _deadLettered.Add(1, new TagList { { "consumer", consumer }, { "topic", topic } });

    public void Published(string topic, string messageType) =>
        _published.Add(1, new TagList { { "topic", topic }, { "message_type", messageType } });

    public void PublishFailed(string topic, string messageType) =>
        _publishFailures.Add(1, new TagList { { "topic", topic }, { "message_type", messageType } });
}
