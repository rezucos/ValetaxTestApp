using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ValetaxTest.EventBus;
using ValetaxTest.EventBus.Abstractions;

namespace ValetaxTest.MessageLogEF;

/// <summary>Adds outgoing messages to the service's DbContext, so they are saved in the same transaction as the data.</summary>
public sealed class EfOutbox<TContext>(TContext dbContext, TimeProvider timeProvider) : IOutbox
    where TContext : DbContext
{
    public void Enqueue(string topic, string key, object message, string? correlationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(message);

        var traceContext = new Dictionary<string, string>();
        TraceContext.InjectCurrent(traceContext);

        dbContext.Set<OutboxMessage>().Add(new OutboxMessage
        {
            MessageId = Guid.CreateVersion7(),
            Topic = topic,
            Key = key,
            // Consumers find the handler by this class name ("message-type" header).
            Type = message.GetType().Name,
            Payload = MessageSerializer.Serialize(message),
            TraceContext = traceContext.Count == 0 ? null : JsonSerializer.Serialize(traceContext),
            CorrelationId = correlationId,
            CreatedAt = timeProvider.GetUtcNow(),
        });
    }
}
