using Microsoft.EntityFrameworkCore;
using ValetaxTest.Wallets.Application.Abstractions;
using ValetaxTest.Wallets.Application.Mappings;
using ValetaxTest.Wallets.Application.Wallets;
using ValetaxTest.Wallets.Domain.Wallets;

namespace ValetaxTest.Wallets.Infrastructure.Persistence.Queries;

public sealed class WalletReader(WalletsDbContext dbContext) : IWalletReader
{
    public async Task<WalletDto?> GetWalletAsync(string userId, CancellationToken cancellationToken)
    {
        var wallet = await dbContext.Wallets.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);

        return wallet?.ToDto();
    }

    public async Task<IReadOnlyList<WalletPayoutDto>> ListPayoutsAsync(string userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var payouts = await dbContext.Payouts.AsNoTracking()
            .Include(payout => payout.Items.OrderBy(item => item.EventId).ThenBy(item => item.Level))
            .Where(payout => payout.UserId == userId && payout.Status == WalletPayoutStatus.Credited)
            .OrderByDescending(payout => payout.ProcessedAt).ThenBy(payout => payout.PayoutId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);

        return payouts.Select(payout => payout.ToDto()).ToList();
    }
}
