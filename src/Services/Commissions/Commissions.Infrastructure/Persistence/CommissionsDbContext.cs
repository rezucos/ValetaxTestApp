using Microsoft.EntityFrameworkCore;
using ValetaxTest.Commissions.Domain.Accrual;
using ValetaxTest.Commissions.Domain.Payouts;
using ValetaxTest.Commissions.Domain.Schemas;
using ValetaxTest.MessageLogEF;

namespace ValetaxTest.Commissions.Infrastructure.Persistence;

public sealed class CommissionsDbContext(DbContextOptions<CommissionsDbContext> options) : DbContext(options)
{
    public DbSet<Commission> Commissions => Set<Commission>();

    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    public DbSet<CommissionSettings> Settings => Set<CommissionSettings>();

    public DbSet<SchemaChange> SchemaChanges => Set<SchemaChange>();

    public DbSet<PayoutReservation> PayoutReservations => Set<PayoutReservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommissionsDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
    }
}
