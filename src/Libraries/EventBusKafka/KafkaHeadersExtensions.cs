using System.Text;
using Confluent.Kafka;

namespace ValetaxTest.EventBusKafka;

public static class KafkaHeadersExtensions
{
    public static Headers AddString(this Headers headers, string key, string value)
    {
        headers.Add(key, Encoding.UTF8.GetBytes(value));
        return headers;
    }

    public static string? GetString(this Headers? headers, string key) =>
        headers is not null && headers.TryGetLastBytes(key, out var bytes) ? Encoding.UTF8.GetString(bytes) : null;
}
