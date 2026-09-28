namespace ValetaxTest.MessageLogEF;

/// <summary>A message processed by a consumer; a message with a known id is skipped.</summary>
public sealed class InboxMessage
{
    public Guid MessageId { get; set; }

    public required string Consumer { get; set; }

    public required string MessageType { get; set; }

    public DateTimeOffset ProcessedAt { get; set; }
}
