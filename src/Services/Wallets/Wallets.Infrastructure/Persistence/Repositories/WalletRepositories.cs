using Microsoft.EntityFrameworkCore;
using ValetaxTest.Wallets.Domain.Wallets;

namespace ValetaxTest.Wallets.Infrastructure.Persistence.Repositories;

public sealed class WalletRepository(WalletsDbContext dbContext) : IWalletRepository
{
    public Task<bool> ExistsAsync(string userId, CancellationToken cancellationToken) =>
        dbContext.Wallets.AnyAsync(wallet => wallet.UserId == userId, cancellationToken);

    public async Task<Wallet?> FindForUpdateAsync(string userId, CancellationToken cancellationToken)
    {
        // The row lock makes balance updates of one wallet run one after another, across all replicas.
        var rows = await dbContext.Wallets
            .FromSql($"SELECT * FROM wallets WHERE user_id = {userId} FOR UPDATE")
            .ToListAsync(cancellationToken);
        return rows.SingleOrDefault();
    }

    public void Add(Wallet wallet) => dbContext.Wallets.Add(wallet);
}

public sealed class WalletPayoutRepository(WalletsDbContext dbContext) : IWalletPayoutRepository
{
    public Task<WalletPayout?> FindAsync(Guid payoutId, CancellationToken cancellationToken) =>
        dbContext.Payouts.SingleOrDefaultAsync(payout => payout.PayoutId == payoutId, cancellationToken);

    public void Add(WalletPayout payout) => dbContext.Payouts.Add(payout);
}
