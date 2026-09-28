using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ValetaxTest.EventBusKafka;

/// <summary>Kafka is available when it answers a metadata request.</summary>
internal sealed class KafkaHealthCheck(IKafkaProducer producer) : IHealthCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = await Task.Run(() =>
            {
                using var adminClient = new DependentAdminClientBuilder(producer.Handle).Build();
                return adminClient.GetMetadata(Timeout);
            }, cancellationToken);

            return metadata.Brokers.Count > 0
                ? HealthCheckResult.Healthy($"{metadata.Brokers.Count} broker(s) available")
                : HealthCheckResult.Unhealthy("No Kafka brokers available");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Kafka is unavailable", exception);
        }
    }
}
