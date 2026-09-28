using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValetaxTest.Core;
using ValetaxTest.Events.Domain.ProfitEvents;
using ValetaxTest.PersistencePostgres;

namespace ValetaxTest.Events.Infrastructure.Persistence.Configurations;

internal sealed class ProfitEventConfiguration : IEntityTypeConfiguration<ProfitEvent>
{
    public void Configure(EntityTypeBuilder<ProfitEvent> builder)
    {
        builder.ToTable("profit_events");
        builder.HasKey(profitEvent => profitEvent.ExternalId);
        builder.Property(profitEvent => profitEvent.ExternalId).HasMaxLength(ExternalIds.EventIdMaxLength);
        builder.Property(profitEvent => profitEvent.UserId).HasMaxLength(ExternalIds.UserIdMaxLength);
        builder.Property(profitEvent => profitEvent.Profit).IsMoney();
        builder.Property(profitEvent => profitEvent.Status).IsEnumString();
        builder.Property(profitEvent => profitEvent.Outcome).IsEnumString();
        builder.Property(profitEvent => profitEvent.SchemaType).IsEnumString();

        builder.HasMany(profitEvent => profitEvent.Commissions)
            .WithOne()
            .HasForeignKey(commission => commission.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // For the list of a user's events, newest first.
        builder.HasIndex(profitEvent => new { profitEvent.UserId, profitEvent.OccurredAt });
    }
}

internal sealed class EventCommissionConfiguration : IEntityTypeConfiguration<EventCommission>
{
    public void Configure(EntityTypeBuilder<EventCommission> builder)
    {
        builder.ToTable("event_commissions");
        builder.HasKey(commission => commission.CommissionId);
        // Ids come from the Commissions service.
        builder.Property(commission => commission.CommissionId).ValueGeneratedNever();
        builder.Property(commission => commission.EventId).HasMaxLength(ExternalIds.EventIdMaxLength);
        builder.Property(commission => commission.PartnerId).HasMaxLength(ExternalIds.UserIdMaxLength);
        builder.Property(commission => commission.Amount).IsMoney();
        builder.Property(commission => commission.SchemaType).IsEnumString();
    }
}

internal sealed class CommissionPaymentConfiguration : IEntityTypeConfiguration<CommissionPayment>
{
    public void Configure(EntityTypeBuilder<CommissionPayment> builder)
    {
        // No foreign key to event_commissions: a payment may be recorded before its commission arrives.
        builder.ToTable("commission_payments");
        builder.HasKey(payment => payment.CommissionId);
        builder.Property(payment => payment.CommissionId).ValueGeneratedNever();
        builder.HasIndex(payment => payment.PayoutId);
    }
}
