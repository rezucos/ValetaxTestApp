using System.Diagnostics.Metrics;
using ValetaxTest.Events.Domain.ProfitEvents;

namespace ValetaxTest.Events.Application;

public sealed class EventsMetrics
{
    public const string MeterName = "ValetaxTest.Events";

    private readonly Counter<long> _registered;
    private readonly Counter<long> _processed;

    public EventsMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _registered = meter.CreateCounter<long>(
            "valetaxtest.events.registered", "{event}", "Profit events accepted (result: created or replayed duplicate)");
        _processed = meter.CreateCounter<long>(
            "valetaxtest.events.processed", "{event}", "Profit events whose commissions were calculated, by outcome");
    }

    public void EventRegistered(bool created) =>
        _registered.Add(1, new KeyValuePair<string, object?>("result", created ? "created" : "replayed"));

    public void EventProcessed(ProcessingOutcome outcome) =>
        _processed.Add(1, new KeyValuePair<string, object?>("outcome", outcome.ToString()));
}
