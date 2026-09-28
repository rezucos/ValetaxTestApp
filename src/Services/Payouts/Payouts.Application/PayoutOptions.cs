namespace ValetaxTest.Payouts.Application;

public sealed class PayoutOptions
{
    public const string SectionName = "Payouts";

    /// <summary>How often accrued commissions are paid out automatically.</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Disables the periodic run (payouts can still be started through the API).</summary>
    public bool SchedulerEnabled { get; set; } = true;

    /// <summary>How long a saga waits for a reply before sending the command again.</summary>
    public TimeSpan StepTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Longest pause between re-sends (the pause doubles each time).</summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>How often to look for sagas that got no reply in time.</summary>
    public TimeSpan TimeoutCheckInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Maximum commissions in one payout; the rest wait for the next run.</summary>
    public int MaxItemsPerPayout { get; set; } = 500;

    /// <summary>Maximum partners paid in one run; the rest wait for the next run.</summary>
    public int MaxPartnersPerRun { get; set; } = 1000;
}

/// <summary>Keys of the PostgreSQL advisory locks of the service.</summary>
internal static class LockKeys
{
    /// <summary>Payout runs of all replicas start payouts one at a time.</summary>
    public const long PayoutRun = 5001;
}
