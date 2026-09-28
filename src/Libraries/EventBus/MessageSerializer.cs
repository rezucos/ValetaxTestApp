using System.Text.Json;
using System.Text.Json.Serialization;

namespace ValetaxTest.EventBus;

/// <summary>Messages are JSON with camelCase names and enums written by name.</summary>
public static class MessageSerializer
{
    private static readonly JsonSerializerOptions Options =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public static string Serialize(object message) =>
        JsonSerializer.Serialize(message, message.GetType(), Options);

    public static object Deserialize(string payload, Type messageType) =>
        JsonSerializer.Deserialize(payload, messageType, Options)
        ?? throw new JsonException($"Payload of {messageType.Name} deserialized to null.");
}
