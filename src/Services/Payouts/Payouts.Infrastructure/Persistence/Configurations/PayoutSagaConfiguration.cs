using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValetaxTest.Core;
using ValetaxTest.Payouts.Domain.Sagas;
using ValetaxTest.PersistencePostgres;

namespace ValetaxTest.Payouts.Infrastructure.Persistence.Configurations;

internal sealed class PayoutSagaConfiguration : IEntityTypeConfiguration<PayoutSaga>
{
    public void Configure(EntityTypeBuilder<PayoutSaga> builder)
    {
        builder.ToTable("payout_sagas");
        builder.HasKey(saga => saga.Id);
        builder.Property(saga => saga.Id).ValueGeneratedNever();
        builder.Property(saga => saga.PartnerId).HasMaxLength(ExternalIds.UserIdMaxLength);
        builder.Property(saga => saga.State).IsEnumString();
        builder.Property(saga => saga.Amount).IsMoney();
        builder.Property(saga => saga.Items).HasJsonConversion();
        builder.Property(saga => saga.History).HasJsonConversion();
        builder.Property(saga => saga.FailureReason).HasMaxLength(500);

        var activeStates = string.Join(", ", PayoutSagaStateMachine.ActiveStates.Select(state => $"'{state}'"));

        // At most one payout in progress per partner: two runs (or two replicas) cannot pay the same partner twice.
        builder.HasIndex(saga => saga.PartnerId)
            .IsUnique()
            .HasFilter($"state IN ({activeStates})")
            .HasDatabaseName("ux_payout_sagas_active_partner");

        // For the timeout monitor: active sagas by the time of their next re-send.
        builder.HasIndex(saga => saga.NextAttemptAt)
            .HasFilter($"state IN ({activeStates})")
            .HasDatabaseName("ix_payout_sagas_next_attempt");

        builder.HasIndex(saga => new { saga.PartnerId, saga.CreatedAt });
    }
}
