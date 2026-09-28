namespace ValetaxTest.EventBus.Abstractions;

/// <summary>
/// Outgoing messages saved in the same transaction as the business data. A background job sends them to
/// Kafka after the commit, so a message is never lost and never sent for changes that were rolled back.
/// </summary>
public interface IOutbox
{
    /// <param name="key">Kafka key: messages with the same key are delivered in the order they were added.</param>
    /// <param name="correlationId">Event or payout id the message belongs to; shows up in the consumer's logs.</param>
    void Enqueue(string topic, string key, object message, string? correlationId = null);
}
