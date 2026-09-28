using ValetaxTest.Commissions.Domain.Schemas;

namespace ValetaxTest.Commissions.Domain.Accrual;

/// <summary>
/// Commission accrued to a partner for a profit event of a user below them in the tree. It is calculated by
/// <see cref="CommissionCalculator"/>; its payout statuses are changed by <c>PayoutReservationService</c>.
/// </summary>
public sealed class Commission
{
    public Guid Id { get; set; }

    public required string EventId { get; set; }

    /// <summary>Owner of the event (the user whose profit generated the commission).</summary>
    public required string SourceUserId { get; set; }

    /// <summary>Partner who receives the commission.</summary>
    public required string PartnerId { get; set; }

    /// <summary>Level of the partner above the event owner (1 = direct partner).</summary>
    public int Level { get; set; }

    /// <summary>Percent of the profit used for this commission.</summary>
    public int RatePercent { get; set; }

    public decimal Amount { get; set; }

    /// <summary>Schema the commission was calculated with (stored as <c>schema_type</c>).</summary>
    public CommissionSchemaType SchemaType { get; set; }

    public CommissionStatus Status { get; set; }

    /// <summary>Payout the commission is reserved for or was paid with.</summary>
    public Guid? PayoutId { get; set; }

    public DateTimeOffset AccruedAt { get; set; }

    public DateTimeOffset? ReservedAt { get; set; }

    public DateTimeOffset? PaidAt { get; set; }
}

public enum CommissionStatus
{
    /// <summary>Accrued, waiting for the next payout.</summary>
    Accrued = 1,

    /// <summary>Included in a payout that is in progress.</summary>
    Reserved = 2,

    /// <summary>Credited to the partner's wallet.</summary>
    Paid = 3,
}
