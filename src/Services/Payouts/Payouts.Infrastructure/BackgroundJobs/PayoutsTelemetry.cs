using System.Diagnostics;

namespace ValetaxTest.Payouts.Infrastructure.BackgroundJobs;

/// <summary>Trace source of the background jobs: every run of a job is a trace of its own.</summary>
public static class PayoutsTelemetry
{
    public const string SourceName = "ValetaxTest.Payouts";

    public static readonly ActivitySource Source = new(SourceName);
}
