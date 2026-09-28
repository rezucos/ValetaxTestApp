using System.Diagnostics;
using OpenTelemetry.Trace;

namespace ValetaxTest.ServiceDefaults;

/// <summary>
/// Drops database and HTTP calls made outside any request or message: the outbox relay, schedulers and
/// health checks poll all the time, and each poll would become a useless one-span trace.
/// Calls made while handling a request or a message are traced as usual.
/// </summary>
internal sealed class SkipOrphanClientSpansSampler : Sampler
{
    private static readonly SamplingResult Drop = new(SamplingDecision.Drop);
    private static readonly SamplingResult Sample = new(SamplingDecision.RecordAndSample);

    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters) =>
        samplingParameters.Kind == ActivityKind.Client ? Drop : Sample;
}
