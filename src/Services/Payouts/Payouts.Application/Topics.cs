namespace ValetaxTest.Payouts.Application;

/// <summary>Kafka topics the service writes to or reads from.</summary>
public static class Topics
{
    /// <summary>Payout saga commands to the Commissions service, key = payout id.</summary>
    public const string CommissionPayoutCommands = "payouts.commission-commands.v1";

    /// <summary>Payout saga commands to the Wallets service, key = user id.</summary>
    public const string WalletPayoutCommands = "payouts.wallet-commands.v1";

    /// <summary>Replies of Commissions and Wallets to the payout saga, key = payout id.</summary>
    public const string PayoutSagaReplies = "payouts.saga-replies.v1";
}
