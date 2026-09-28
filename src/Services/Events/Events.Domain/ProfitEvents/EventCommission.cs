namespace ValetaxTest.Events.Domain.ProfitEvents;

/// <summary>Commission accrued for a profit event, as reported by the Commissions service.</summary>
public sealed class EventCommission
{
    public Guid CommissionId { get; set; }

    public required string EventId { get; set; }

    /// <summary>Partner who receives the commission.</summary>
    public required string PartnerId { get; set; }

    /// <summary>Level of the partner above the event owner (1 = direct partner).</summary>
    public int Level { get; set; }

    public int RatePercent { get; set; }

    public decimal Amount { get; set; }

    public CommissionSchemaType SchemaType { get; set; }
}

/// <summary>
/// Payment of a commission to the partner's wallet. Kept apart from <see cref="EventCommission"/>:
/// the payment message comes from another topic and may be handled before the commission itself.
/// </summary>
public sealed class CommissionPayment
{
    public Guid CommissionId { get; set; }

    public Guid PayoutId { get; set; }

    public DateTimeOffset PaidAt { get; set; }
}
