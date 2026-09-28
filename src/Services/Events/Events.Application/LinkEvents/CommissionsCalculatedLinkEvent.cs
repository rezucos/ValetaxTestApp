// Each service that sends or receives this message has its own copy of this file:
// the class and property names must be the same in all copies.

namespace ValetaxTest.Events.Application.LinkEvents;

/// <summary>Result of the commission calculation for a profit event. Events keeps it for event details.</summary>
public sealed record CommissionsCalculatedLinkEvent(
    string EventId,
    string UserId,
    CommissionCalculationOutcome Outcome,
    CommissionSchemaType? SchemaType,
    IReadOnlyList<CalculatedCommission> Commissions,
    DateTimeOffset CalculatedAt);

public sealed record CalculatedCommission(
    Guid CommissionId,
    string PartnerId,
    int Level,
    int RatePercent,
    decimal Amount,
    CommissionSchemaType SchemaType);

public enum CommissionCalculationOutcome
{
    /// <summary>Commissions were accrued to the partners above the event owner.</summary>
    Accrued = 1,

    /// <summary>Profit is zero or negative, or too small to give a non-zero commission.</summary>
    NotEligible = 2,

    /// <summary>The event owner has no partners.</summary>
    NoPartners = 3,

    /// <summary>The event owner is not registered in the partner program.</summary>
    UnknownUser = 4,
}
