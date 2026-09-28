namespace ValetaxTest.Wallets.Application.Wallets;

/// <summary>Wallets of partners: balances, payout history and administration.</summary>
public interface IWalletService
{
    Task<WalletDto> GetAsync(string userId, CancellationToken cancellationToken);

    /// <summary>Credited payouts of the user with their commissions, newest first; pages start from 1.</summary>
    Task<IReadOnlyList<WalletPayoutDto>> ListPayoutsAsync(string userId, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Blocks the wallet: payouts to it are rejected, and their commissions stay accrued.</summary>
    Task<WalletDto> BlockAsync(string userId, CancellationToken cancellationToken);

    Task<WalletDto> UnblockAsync(string userId, CancellationToken cancellationToken);
}
