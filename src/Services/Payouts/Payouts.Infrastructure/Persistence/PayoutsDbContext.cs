using Microsoft.EntityFrameworkCore;
using ValetaxTest.MessageLogEF;
using ValetaxTest.Payouts.Domain.Sagas;

namespace ValetaxTest.Payouts.Infrastructure.Persistence;

public sealed class PayoutsDbContext(DbContextOptions<PayoutsDbContext> options) : DbContext(options)
{
    public DbSet<PayoutSaga> Sagas => Set<PayoutSaga>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PayoutsDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
    }
}
