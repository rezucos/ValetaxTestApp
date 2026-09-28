using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValetaxTest.Commissions.Domain.Accrual;
using ValetaxTest.Commissions.Domain.Payouts;
using ValetaxTest.Commissions.Domain.Schemas;
using ValetaxTest.Core;
using ValetaxTest.PersistencePostgres;

namespace ValetaxTest.Commissions.Infrastructure.Persistence.Configurations;

internal sealed class CommissionConfiguration : IEntityTypeConfiguration<Commission>
{
    public void Configure(EntityTypeBuilder<Commission> builder)
    {
        builder.ToTable("commissions");
        builder.HasKey(commission => commission.Id);
        builder.Property(commission => commission.Id).ValueGeneratedNever();
        builder.Property(commission => commission.EventId).HasMaxLength(ExternalIds.EventIdMaxLength);
        builder.Property(commission => commission.SourceUserId).HasMaxLength(ExternalIds.UserIdMaxLength);
        builder.Property(commission => commission.PartnerId).HasMaxLength(ExternalIds.UserIdMaxLength);
        builder.Property(commission => commission.Amount).IsMoney();
        // Every commission stores the schema it was calculated with.
        builder.Property(commission => commission.SchemaType).IsEnumString().HasColumnName("schema_type");
        builder.Property(commission => commission.Status).IsEnumString();

        // One commission per level of an event: the same event cannot be accrued twice.
        builder.HasIndex(commission => new { commission.EventId, commission.Level }).IsUnique();

        // For payouts: accrued commissions of a partner, oldest first; other statuses are not in the index.
        builder.HasIndex(commission => new { commission.PartnerId, commission.AccruedAt })
            .HasFilter("status = 'Accrued'")
            .HasDatabaseName("ix_commissions_accrued_by_partner");

        builder.HasIndex(commission => commission.PayoutId);
    }
}

internal sealed class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        builder.ToTable("processed_events");
        builder.HasKey(processedEvent => processedEvent.EventId);
        builder.Property(processedEvent => processedEvent.EventId).HasMaxLength(ExternalIds.EventIdMaxLength);
        builder.Property(processedEvent => processedEvent.UserId).HasMaxLength(ExternalIds.UserIdMaxLength);
        builder.Property(processedEvent => processedEvent.Profit).IsMoney();
        builder.Property(processedEvent => processedEvent.CommissionsTotal).IsMoney();
        builder.Property(processedEvent => processedEvent.Outcome).IsEnumString();
        builder.Property(processedEvent => processedEvent.SchemaType).IsEnumString().HasColumnName("schema_type");
    }
}

internal sealed class CommissionSettingsConfiguration : IEntityTypeConfiguration<CommissionSettings>
{
    public void Configure(EntityTypeBuilder<CommissionSettings> builder)
    {
        builder.ToTable("commission_settings", table =>
            table.HasCheckConstraint("ck_commission_settings_singleton", $"id = {CommissionSettings.SingletonId}"));
        builder.HasKey(settings => settings.Id);
        builder.Property(settings => settings.Id).ValueGeneratedNever();
        builder.Property(settings => settings.SchemaType).IsEnumString().HasColumnName("schema_type");

        // The program starts with the linear schema.
        builder.HasData(new CommissionSettings
        {
            Id = CommissionSettings.SingletonId,
            SchemaType = CommissionSchemaType.Linear,
            UpdatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        });
    }
}

internal sealed class SchemaChangeConfiguration : IEntityTypeConfiguration<SchemaChange>
{
    public void Configure(EntityTypeBuilder<SchemaChange> builder)
    {
        builder.ToTable("schema_changes");
        builder.HasKey(change => change.Id);
        builder.Property(change => change.Id).UseIdentityAlwaysColumn();
        builder.Property(change => change.PreviousSchemaType).IsEnumString();
        builder.Property(change => change.SchemaType).IsEnumString().HasColumnName("schema_type");
    }
}

internal sealed class PayoutReservationConfiguration : IEntityTypeConfiguration<PayoutReservation>
{
    public void Configure(EntityTypeBuilder<PayoutReservation> builder)
    {
        builder.ToTable("payout_reservations");
        builder.HasKey(reservation => reservation.PayoutId);
        builder.Property(reservation => reservation.PayoutId).ValueGeneratedNever();
        builder.Property(reservation => reservation.PartnerId).HasMaxLength(ExternalIds.UserIdMaxLength);
        builder.Property(reservation => reservation.Status).IsEnumString();
        builder.Property(reservation => reservation.TotalAmount).IsMoney();
        builder.Property(reservation => reservation.Items).HasJsonConversion();
        builder.Property(reservation => reservation.ReleaseReason).HasMaxLength(500);
    }
}
