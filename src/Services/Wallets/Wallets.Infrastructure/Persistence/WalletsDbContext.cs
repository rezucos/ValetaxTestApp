using Microsoft.EntityFrameworkCore;
using ValetaxTest.MessageLogEF;
using ValetaxTest.Wallets.Domain.Wallets;

namespace ValetaxTest.Wallets.Infrastructure.Persistence;

public sealed class WalletsDbContext(DbContextOptions<WalletsDbContext> options) : DbContext(options)
{
    public DbSet<Wallet> Wallets => Set<Wallet>();

    public DbSet<WalletPayout> Payouts => Set<WalletPayout>();

    public DbSet<WalletPayoutItem> PayoutItems => Set<WalletPayoutItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WalletsDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
    }
}
