using System.Diagnostics.Metrics;
using ValetaxTest.Wallets.Domain.Wallets;

namespace ValetaxTest.Wallets.Application;

public sealed class WalletsMetrics
{
    public const string MeterName = "ValetaxTest.Wallets";

    private readonly Counter<long> _payouts;
    private readonly Counter<double> _credited;

    public WalletsMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _payouts = meter.CreateCounter<long>(
            "valetaxtest.wallets.payouts", "{payout}", "Payout commands processed by wallets, by status");
        _credited = meter.CreateCounter<double>(
            "valetaxtest.wallets.credited_amount", "{unit}", "Total amount credited to wallets");
    }

    public void PayoutProcessed(WalletPayout payout)
    {
        _payouts.Add(1, new KeyValuePair<string, object?>("status", payout.Status.ToString()));
        if (payout.Status == WalletPayoutStatus.Credited)
        {
            _credited.Add((double)payout.Amount);
        }
    }
}
