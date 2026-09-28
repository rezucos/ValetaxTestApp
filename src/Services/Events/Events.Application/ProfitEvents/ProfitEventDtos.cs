using ValetaxTest.Events.Domain.ProfitEvents;

namespace ValetaxTest.Events.Application.ProfitEvents;

/// <summary>Event without commission details (list view).</summary>
public sealed record ProfitEventDto(
    string ExternalId,
    string UserId,
    decimal Profit,
    DateTimeOffset OccurredAt,
    DateTimeOffset ReceivedAt,
    ProfitEventStatus Status);

/// <param name="Created"><c>false</c> when the request repeated an already registered event.</param>
public sealed record RegisterProfitEventResult(ProfitEventDto Event, bool Created);

/// <summary>Event with all commissions accrued for it.</summary>
public sealed record ProfitEventDetailsDto(
    string ExternalId,
    string UserId,
    decimal Profit,
    DateTimeOffset OccurredAt,
    DateTimeOffset ReceivedAt,
    ProfitEventStatus Status,
    ProcessingOutcome? Outcome,
    CommissionSchemaType? SchemaType,
    DateTimeOffset? ProcessedAt,
    decimal CommissionsTotal,
    IReadOnlyList<EventCommissionDto> Commissions);

/// <param name="Level">Level of the partner above the event owner (1 = direct partner).</param>
/// <param name="RatePercent">Percent of the profit: L for the linear schema, F(L) for the Fibonacci one.</param>
public sealed record EventCommissionDto(
    Guid CommissionId,
    string PartnerId,
    int Level,
    int RatePercent,
    decimal Amount,
    CommissionSchemaType SchemaType,
    bool IsPaid,
    DateTimeOffset? PaidAt,
    Guid? PayoutId);
