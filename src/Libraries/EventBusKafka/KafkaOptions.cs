namespace ValetaxTest.EventBusKafka;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>Prefix of Kafka client ids, usually the service name.</summary>
    public string ClientId { get; set; } = "valetaxtest";
}
