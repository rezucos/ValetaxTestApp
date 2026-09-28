using Microsoft.EntityFrameworkCore;
using ValetaxTest.Events.Domain.ProfitEvents;
using ValetaxTest.MessageLogEF;

namespace ValetaxTest.Events.Infrastructure.Persistence;

public sealed class EventsDbContext(DbContextOptions<EventsDbContext> options) : DbContext(options)
{
    public DbSet<ProfitEvent> ProfitEvents => Set<ProfitEvent>();

    public DbSet<EventCommission> EventCommissions => Set<EventCommission>();

    public DbSet<CommissionPayment> CommissionPayments => Set<CommissionPayment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventsDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
    }
}
