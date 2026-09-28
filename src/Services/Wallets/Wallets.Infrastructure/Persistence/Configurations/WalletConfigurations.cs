using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValetaxTest.Core;
using ValetaxTest.PersistencePostgres;
using ValetaxTest.Wallets.Domain.Wallets;

namespace ValetaxTest.Wallets.Infrastructure.Persistence.Configurations;

internal sealed class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("wallets", table => table.HasCheckConstraint("ck_wallets_balance_not_negative", "balance >= 0"));
        builder.HasKey(wallet => wallet.UserId);
        builder.Property(wallet => wallet.UserId).HasMaxLength(ExternalIds.UserIdMaxLength);
        builder.Property(wallet => wallet.Balance).IsMoney();
        builder.Property(wallet => wallet.Status).IsEnumString();
    }
}

internal sealed class WalletPayoutConfiguration : IEntityTypeConfiguration<WalletPayout>
{
    public void Configure(EntityTypeBuilder<WalletPayout> builder)
    {
        // The payout id is the key: a payout is processed once, and a repeated command gets the stored result.
        builder.ToTable("wallet_payouts");
        builder.HasKey(payout => payout.PayoutId);
        builder.Property(payout => payout.PayoutId).ValueGeneratedNever();
        builder.Property(payout => payout.UserId).HasMaxLength(ExternalIds.UserIdMaxLength);
        builder.Property(payout => payout.Status).IsEnumString();
        builder.Property(payout => payout.Amount).IsMoney();
        builder.Property(payout => payout.RejectionReason).HasMaxLength(500);

        builder.HasMany(payout => payout.Items)
            .WithOne()
            .HasForeignKey(item => item.PayoutId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(payout => new { payout.UserId, payout.ProcessedAt });
    }
}

internal sealed class WalletPayoutItemConfiguration : IEntityTypeConfiguration<WalletPayoutItem>
{
    public void Configure(EntityTypeBuilder<WalletPayoutItem> builder)
    {
        // The commission id is the key: a commission can reach a wallet only once, whatever happens in other services.
        builder.ToTable("wallet_payout_items");
        builder.HasKey(item => item.CommissionId);
        builder.Property(item => item.CommissionId).ValueGeneratedNever();
        builder.Property(item => item.EventId).HasMaxLength(ExternalIds.EventIdMaxLength);
        builder.Property(item => item.Amount).IsMoney();
        builder.Property(item => item.SchemaType).IsEnumString().HasColumnName("schema_type");
    }
}
