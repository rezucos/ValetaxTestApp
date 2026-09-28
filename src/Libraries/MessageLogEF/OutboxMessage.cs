namespace ValetaxTest.MessageLogEF;

/// <summary>A message saved together with the business data and waiting to be sent to Kafka.</summary>
public sealed class OutboxMessage
{
    /// <summary>Growing id: messages are sent in the order they were saved.</summary>
    public long Id { get; set; }

    public Guid MessageId { get; set; }

    public required string Topic { get; set; }

    public required string Key { get; set; }

    public required string Type { get; set; }

    public required string Payload { get; set; }

    /// <summary>Trace of the request that created the message.</summary>
    public string? TraceContext { get; set; }

    public string? CorrelationId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public int Attempts { get; set; }

    public string? LastError { get; set; }
}
