namespace ValetaxTest.Events.Domain.ProfitEvents;

/// <summary>
/// Profit or loss of a user reported by an external system.
/// The rules are in <see cref="ProfitEventService"/>.
/// </summary>
public sealed class ProfitEvent
{
    /// <summary>Id issued by the external system; an event with the same id is registered only once.</summary>
    public required string ExternalId { get; set; }

    public required string UserId { get; set; }

    /// <summary>Positive for profit, negative for loss (conventional units).</summary>
    public decimal Profit { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public DateTimeOffset ReceivedAt { get; set; }

    public ProfitEventStatus Status { get; set; }

    /// <summary>Result of the commission calculation, known once the event is processed.</summary>
    public ProcessingOutcome? Outcome { get; set; }

    /// <summary>Schema the commissions of this event were calculated with (only when commissions were accrued).</summary>
    public CommissionSchemaType? SchemaType { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }

    /// <summary>Commissions accrued for this event to the partners above its owner.</summary>
    public List<EventCommission> Commissions { get; set; } = [];
}

public enum ProfitEventStatus
{
    /// <summary>Accepted, commissions are being calculated.</summary>
    Pending = 1,

    /// <summary>Commissions are calculated (possibly none).</summary>
    Processed = 2,
}

public enum ProcessingOutcome
{
    CommissionsAccrued = 1,
    NotEligible = 2,
    NoPartners = 3,
    UnknownUser = 4,
}

public enum CommissionSchemaType
{
    Linear = 1,
    Fibonacci = 2,
}
