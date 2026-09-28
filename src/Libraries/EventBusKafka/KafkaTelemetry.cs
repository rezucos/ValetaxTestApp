using System.Diagnostics;
using System.Globalization;
using System.Text;
using Confluent.Kafka;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using ValetaxTest.MessageLogEF;

namespace ValetaxTest.EventBusKafka;

/// <summary>
/// Tracing through Kafka: the trace travels in message headers, so an HTTP request and all the message
/// handlers it leads to show up as one trace.
/// </summary>
public static class KafkaTelemetry
{
    public const string SourceName = "ValetaxTest.Messaging";

    private static readonly ActivitySource Source = new(SourceName);

    public static PropagationContext ExtractContext(Headers? headers) =>
        headers is null
            ? default
            : TraceContext.Propagator.Extract(default, headers,
                static (source, key) => source.GetString(key) is { } value ? [value] : []);

    /// <summary>Writes the trace context of <paramref name="activity"/> (or the stored one) into Kafka headers.</summary>
    public static void InjectContext(Activity? activity, IReadOnlyDictionary<string, string>? storedContext, Headers headers)
    {
        if (activity is not null)
        {
            TraceContext.Propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), headers,
                static (target, key, value) =>
                {
                    target.Remove(key);
                    target.Add(key, Encoding.UTF8.GetBytes(value));
                });
            return;
        }

        if (storedContext is null)
        {
            return;
        }

        foreach (var (key, value) in storedContext)
        {
            headers.Remove(key);
            headers.Add(key, Encoding.UTF8.GetBytes(value));
        }
    }

    public static Activity? StartPublish(string topic, string messageType, Guid messageId, PropagationContext parent)
    {
        var activity = Source.StartActivity($"{topic} publish", ActivityKind.Producer, parent.ActivityContext);
        activity?
            .SetTag("messaging.system", "kafka")
            .SetTag("messaging.operation.type", "send")
            .SetTag("messaging.destination.name", topic)
            .SetTag("messaging.message.id", messageId.ToString())
            .SetTag("messaging.message.type", messageType);
        return activity;
    }

    public static Activity? StartProcess(ConsumeResult<string, string> result, string consumerGroup, string? messageType, PropagationContext parent)
    {
        var activity = Source.StartActivity($"{result.Topic} process", ActivityKind.Consumer, parent.ActivityContext);
        activity?
            .SetTag("messaging.system", "kafka")
            .SetTag("messaging.operation.type", "process")
            .SetTag("messaging.destination.name", result.Topic)
            .SetTag("messaging.consumer.group.name", consumerGroup)
            .SetTag("messaging.destination.partition.id", result.Partition.Value.ToString(CultureInfo.InvariantCulture))
            .SetTag("messaging.kafka.offset", result.Offset.Value)
            .SetTag("messaging.kafka.message.key", result.Message.Key)
            .SetTag("messaging.message.type", messageType);
        return activity;
    }
}
