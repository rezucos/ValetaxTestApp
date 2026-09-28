namespace ValetaxTest.EventBus.Abstractions;

/// <summary>Kafka details of the message being handled: id, type, topic, key, partition and offset.</summary>
public sealed record MessageContext(
    Guid MessageId,
    string MessageType,
    string Topic,
    string? Key,
    string? CorrelationId,
    int Partition,
    long Offset);
