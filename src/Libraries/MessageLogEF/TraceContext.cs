using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace ValetaxTest.MessageLogEF;

/// <summary>
/// Saves the current trace with an outbox message, so the message is sent later as part of the trace
/// of the request that created it.
/// </summary>
public static class TraceContext
{
    public static TextMapPropagator Propagator { get; } =
        new CompositeTextMapPropagator([new TraceContextPropagator(), new BaggagePropagator()]);

    public static void InjectCurrent(IDictionary<string, string> carrier)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        Propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), carrier,
            static (target, key, value) => target[key] = value);
    }

    public static PropagationContext Extract(IReadOnlyDictionary<string, string>? carrier) =>
        carrier is null
            ? default
            : Propagator.Extract(default, carrier,
                static (source, key) => source.TryGetValue(key, out var value) ? [value] : []);
}
