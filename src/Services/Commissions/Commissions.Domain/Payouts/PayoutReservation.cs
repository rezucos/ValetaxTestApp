using ValetaxTest.Commissions.Domain.Schemas;

namespace ValetaxTest.Commissions.Domain.Payouts;

/// <summary>
/// Commissions reserved for a payout. There is one reservation per payout id, so a repeated saga command
/// is answered from it instead of being executed again.
/// </summary>
public sealed class PayoutReservation
{
    public Guid PayoutId { get; set; }

    public required string PartnerId { get; set; }

    public ReservationStatus Status { get; set; }

    public decimal TotalAmount { get; set; }

    /// <summary>The reserved commissions as they were at the moment of reservation.</summary>
    public List<ReservedCommission> Items { get; set; } = [];

    public DateTimeOffset ReservedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string? ReleaseReason { get; set; }
}

public sealed record ReservedCommission(Guid CommissionId, string EventId, int Level, decimal Amount, CommissionSchemaType SchemaType);

public enum ReservationStatus
{
    /// <summary>Commissions are set aside while the wallet is being credited.</summary>
    Reserved = 1,

    /// <summary>The wallet was credited, the commissions are paid.</summary>
    Paid = 2,

    /// <summary>The payout was cancelled: the commissions went back to "accrued".</summary>
    Released = 3,
}

public interface IPayoutReservationRepository
{
    /// <summary>Loads the reservation to change it; the row stays locked until the transaction ends.</summary>
    Task<PayoutReservation?> FindForUpdateAsync(Guid payoutId, CancellationToken cancellationToken);

    void Add(PayoutReservation reservation);
}
