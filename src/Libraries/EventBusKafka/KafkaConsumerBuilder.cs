using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ValetaxTest.EventBus.Abstractions;

namespace ValetaxTest.EventBusKafka;

/// <summary>One consumer group: its topics and a handler for each message type.</summary>
internal sealed class ConsumerRegistration(string name)
{
    /// <summary>Consumer group id; the inbox is kept per group, so each group skips only its own duplicates.</summary>
    public string Name { get; } = name;

    public List<string> Topics { get; } = [];

    public Dictionary<string, HandlerDescriptor> Handlers { get; } = new(StringComparer.Ordinal);
}

internal sealed record HandlerDescriptor(
    Type MessageType,
    Func<IServiceProvider, object, MessageContext, CancellationToken, Task> Handle);

public sealed class KafkaConsumerBuilder
{
    private readonly IServiceCollection _services;
    private readonly ConsumerRegistration _registration;

    internal KafkaConsumerBuilder(IServiceCollection services, ConsumerRegistration registration)
    {
        _services = services;
        _registration = registration;
    }

    /// <summary>Subscribes to <paramref name="topic"/> and sends <typeparamref name="TMessage"/> messages to <typeparamref name="THandler"/>.</summary>
    public KafkaConsumerBuilder Handle<TMessage, THandler>(string topic)
        where TMessage : class
        where THandler : class, IMessageHandler<TMessage>
    {
        _services.TryAddScoped<THandler>();

        if (!_registration.Topics.Contains(topic))
        {
            _registration.Topics.Add(topic);
        }

        // The producer writes the class name to the "message-type" header.
        var messageType = typeof(TMessage).Name;
        var descriptor = new HandlerDescriptor(
            typeof(TMessage),
            static (provider, message, context, cancellationToken) =>
                provider.GetRequiredService<THandler>().HandleAsync((TMessage)message, context, cancellationToken));

        if (!_registration.Handlers.TryAdd(messageType, descriptor))
        {
            throw new InvalidOperationException($"Consumer '{_registration.Name}' already has a handler for {messageType}.");
        }

        return this;
    }
}
