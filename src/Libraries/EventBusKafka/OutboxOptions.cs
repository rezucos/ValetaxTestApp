namespace ValetaxTest.EventBusKafka;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    /// <summary>Pause between polls when the outbox is empty.</summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMilliseconds(200);

    public int BatchSize { get; set; } = 100;

    /// <summary>Time limit for sending one batch; shutdown waits for a started batch at most this long.</summary>
    public TimeSpan BatchTimeout { get; set; } = TimeSpan.FromSeconds(15);
}
