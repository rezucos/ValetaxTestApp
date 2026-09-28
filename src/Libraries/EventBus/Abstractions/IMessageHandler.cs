namespace ValetaxTest.EventBus.Abstractions;

/// <summary>
/// Handles messages of one type. It is called inside a database transaction that also records the message
/// as processed, so the same Kafka message is never handled twice. The same request can still arrive in a
/// new message (a saga command sent again after a timeout), and the handler must cope with that.
/// </summary>
public interface IMessageHandler<in TMessage> where TMessage : class
{
    Task HandleAsync(TMessage message, MessageContext context, CancellationToken cancellationToken);
}
