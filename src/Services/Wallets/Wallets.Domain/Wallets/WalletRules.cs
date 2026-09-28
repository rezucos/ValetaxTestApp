namespace ValetaxTest.Wallets.Domain.Wallets;

/// <summary>
/// Rules of wallets and payouts. Not called WalletService: that name belongs to the service that loads
/// and saves wallets.
/// </summary>
public sealed class WalletRules
{
    public Wallet Open(string userId, DateTimeOffset now) => new()
    {
        UserId = userId,
        Balance = 0,
        Status = WalletStatus.Active,
        CreatedAt = now,
        UpdatedAt = now,
    };

    /// <summary>
    /// Credits the commissions to the wallet. A blocked wallet or a broken payout does not throw: the payout
    /// is saved as rejected, and the saga returns its commissions to "accrued".
    /// </summary>
    public WalletPayout Credit(Wallet wallet, Guid payoutId, IReadOnlyList<WalletPayoutItem> items, DateTimeOffset now)
    {
        var rejection = wallet.Status == WalletStatus.Blocked ? "Wallet is blocked"
            : items.Count == 0 ? "Payout contains no commissions"
            : items.Any(item => item.Amount <= 0) ? "Payout contains non-positive amounts"
            : items.Select(item => item.CommissionId).Distinct().Count() != items.Count ? "Payout contains duplicate commissions"
            : null;

        if (rejection is not null)
        {
            return new WalletPayout
            {
                PayoutId = payoutId,
                UserId = wallet.UserId,
                Status = WalletPayoutStatus.Rejected,
                Amount = 0,
                RejectionReason = rejection,
                ProcessedAt = now,
            };
        }

        var amount = items.Sum(item => item.Amount);
        wallet.Balance += amount;
        wallet.UpdatedAt = now;

        foreach (var item in items)
        {
            item.PayoutId = payoutId;
        }

        return new WalletPayout
        {
            PayoutId = payoutId,
            UserId = wallet.UserId,
            Status = WalletPayoutStatus.Credited,
            Amount = amount,
            ProcessedAt = now,
            Items = items.ToList(),
        };
    }

    /// <returns><c>false</c> when the wallet is already blocked.</returns>
    public bool Block(Wallet wallet, DateTimeOffset now)
    {
        if (wallet.Status == WalletStatus.Blocked)
        {
            return false;
        }

        wallet.Status = WalletStatus.Blocked;
        wallet.UpdatedAt = now;
        return true;
    }

    /// <returns><c>false</c> when the wallet is already active.</returns>
    public bool Unblock(Wallet wallet, DateTimeOffset now)
    {
        if (wallet.Status == WalletStatus.Active)
        {
            return false;
        }

        wallet.Status = WalletStatus.Active;
        wallet.UpdatedAt = now;
        return true;
    }
}
