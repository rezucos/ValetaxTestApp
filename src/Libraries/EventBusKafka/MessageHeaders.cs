namespace ValetaxTest.EventBusKafka;

/// <summary>Kafka headers written with every message.</summary>
public static class MessageHeaders
{
    /// <summary>Unique message id; consumers use it to skip messages they have already processed.</summary>
    public const string MessageId = "message-id";

    /// <summary>Class name of the message; lets several message types share one topic.</summary>
    public const string MessageType = "message-type";

    /// <summary>Event or payout id the message belongs to; written to logs.</summary>
    public const string CorrelationId = "correlation-id";

    public const string OccurredAt = "occurred-at";

    public const string DeadLetterReason = "dlt-reason";
    public const string DeadLetterError = "dlt-error";
    public const string DeadLetterOriginalTopic = "dlt-original-topic";
    public const string DeadLetterOriginalPartition = "dlt-original-partition";
    public const string DeadLetterOriginalOffset = "dlt-original-offset";
    public const string DeadLetterConsumer = "dlt-consumer";
    public const string DeadLetterFailedAt = "dlt-failed-at";
}
