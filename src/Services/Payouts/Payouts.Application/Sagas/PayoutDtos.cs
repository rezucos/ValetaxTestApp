using ValetaxTest.Payouts.Domain.Sagas;

namespace ValetaxTest.Payouts.Application.Sagas;

public sealed record PayoutSummaryDto(
    Guid PayoutId,
    string PartnerId,
    PayoutSagaState State,
    decimal Amount,
    int CommissionsCount,
    string? FailureReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt);

public sealed record PayoutDetailsDto(
    Guid PayoutId,
    string PartnerId,
    PayoutSagaState State,
    decimal Amount,
    string? FailureReason,
    DateTimeOffset? PaidAt,
    int StepAttempts,
    DateTimeOffset? NextAttemptAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<PayoutSagaItemDto> Commissions,
    IReadOnlyList<PayoutSagaTransitionDto> History);

public sealed record PayoutSagaItemDto(Guid CommissionId, string EventId, int Level, decimal Amount, CommissionSchemaType SchemaType);

/// <param name="From"><c>null</c> for the start of the payout.</param>
public sealed record PayoutSagaTransitionDto(DateTimeOffset At, PayoutSagaState? From, PayoutSagaState To, string Note);

/// <param name="PartnersWithAccruedCommissions">Partners found with commissions waiting for a payout.</param>
/// <param name="StartedPayouts">Payouts started by this run (partners with a payout in progress are skipped).</param>
public sealed record PayoutRunResult(int PartnersWithAccruedCommissions, IReadOnlyList<StartedPayout> StartedPayouts);

public sealed record StartedPayout(Guid PayoutId, string PartnerId);
