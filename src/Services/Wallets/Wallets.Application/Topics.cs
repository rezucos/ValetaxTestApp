namespace ValetaxTest.Wallets.Application;

/// <summary>Kafka topics the service writes to or reads from.</summary>
public static class Topics
{
    /// <summary><see cref="LinkEvents.UserRegisteredLinkEvent"/>, key = user id.</summary>
    public const string UserRegistered = "partners.user-registered.v1";

    /// <summary>Payout saga commands to the Wallets service, key = user id.</summary>
    public const string WalletPayoutCommands = "payouts.wallet-commands.v1";

    /// <summary>Replies of Commissions and Wallets to the payout saga, key = payout id.</summary>
    public const string PayoutSagaReplies = "payouts.saga-replies.v1";
}
