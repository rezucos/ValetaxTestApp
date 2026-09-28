namespace ValetaxTest.Commissions.Application;

/// <summary>Kafka topics the service writes to or reads from.</summary>
public static class Topics
{
    /// <summary><see cref="LinkEvents.ProfitEventRegisteredLinkEvent"/>, key = event id.</summary>
    public const string ProfitEventRegistered = "events.profit-event-registered.v1";

    /// <summary><see cref="LinkEvents.CommissionsCalculatedLinkEvent"/>, key = event id.</summary>
    public const string CommissionsCalculated = "commissions.commissions-calculated.v1";

    /// <summary><see cref="LinkEvents.CommissionsPaidLinkEvent"/>, key = payout id.</summary>
    public const string CommissionsPaid = "commissions.commissions-paid.v1";

    /// <summary>Payout saga commands to the Commissions service, key = payout id.</summary>
    public const string CommissionPayoutCommands = "payouts.commission-commands.v1";

    /// <summary>Replies of Commissions and Wallets to the payout saga, key = payout id.</summary>
    public const string PayoutSagaReplies = "payouts.saga-replies.v1";
}
