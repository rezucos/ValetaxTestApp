namespace ValetaxTest.Wallets.Domain.Wallets;

public interface IWalletRepository
{
    Task<bool> ExistsAsync(string userId, CancellationToken cancellationToken);

    /// <summary>Loads the wallet to change it; the row stays locked until the transaction ends.</summary>
    Task<Wallet?> FindForUpdateAsync(string userId, CancellationToken cancellationToken);

    void Add(Wallet wallet);
}

public interface IWalletPayoutRepository
{
    Task<WalletPayout?> FindAsync(Guid payoutId, CancellationToken cancellationToken);

    void Add(WalletPayout payout);
}
