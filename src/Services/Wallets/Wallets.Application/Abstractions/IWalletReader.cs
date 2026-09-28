using ValetaxTest.Wallets.Application.Wallets;

namespace ValetaxTest.Wallets.Application.Abstractions;

/// <summary>Queries of wallets.</summary>
public interface IWalletReader
{
    Task<WalletDto?> GetWalletAsync(string userId, CancellationToken cancellationToken);

    /// <summary>Credited payouts of the user with their commissions, newest first; pages start from 1.</summary>
    Task<IReadOnlyList<WalletPayoutDto>> ListPayoutsAsync(string userId, int page, int pageSize, CancellationToken cancellationToken);
}
