using ValetaxTest.Wallets.Application.Wallets;
using ValetaxTest.Wallets.Domain.Wallets;

namespace ValetaxTest.Wallets.Application.Mappings;

public static class DomainToDtoMapper
{
    extension(Wallet wallet)
    {
        public WalletDto ToDto() => new(wallet.UserId, wallet.Balance, wallet.Status, wallet.CreatedAt, wallet.UpdatedAt);
    }

    extension(WalletPayout payout)
    {
        public WalletPayoutDto ToDto() => new(
            payout.PayoutId,
            payout.Amount,
            payout.ProcessedAt,
            payout.Items.Select(item => item.ToDto()).ToList());
    }

    extension(WalletPayoutItem item)
    {
        public WalletPayoutItemDto ToDto() => new(item.CommissionId, item.EventId, item.Level, item.Amount, item.SchemaType);
    }
}
