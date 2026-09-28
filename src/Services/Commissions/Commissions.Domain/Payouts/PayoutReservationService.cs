using ValetaxTest.Commissions.Domain.Accrual;
using ValetaxTest.Core;

namespace ValetaxTest.Commissions.Domain.Payouts;

/// <summary>
/// Changes commission statuses during a payout: accrued commissions are reserved, then either marked
/// as paid or returned to accrued.
/// </summary>
public sealed class PayoutReservationService
{
    public PayoutReservation Reserve(Guid payoutId, string partnerId, IReadOnlyList<Commission> commissions, DateTimeOffset now)
    {
        foreach (var commission in commissions)
        {
            if (commission.Status != CommissionStatus.Accrued || commission.PartnerId != partnerId)
            {
                throw new DomainException(
                    $"Commission {commission.Id} ({commission.Status}, partner '{commission.PartnerId}') cannot be reserved for partner '{partnerId}'.");
            }

            commission.Status = CommissionStatus.Reserved;
            commission.PayoutId = payoutId;
            commission.ReservedAt = now;
        }

        return new PayoutReservation
        {
            PayoutId = payoutId,
            PartnerId = partnerId,
            Status = ReservationStatus.Reserved,
            TotalAmount = commissions.Sum(commission => commission.Amount),
            Items = commissions
                .Select(commission => new ReservedCommission(
                    commission.Id, commission.EventId, commission.Level, commission.Amount, commission.SchemaType))
                .ToList(),
            ReservedAt = now,
        };
    }

    /// <summary>Marks the reserved commissions as paid.</summary>
    /// <returns><c>false</c> when the payout was already confirmed.</returns>
    public bool ConfirmPaid(PayoutReservation reservation, IReadOnlyList<Commission> commissions, DateTimeOffset paidAt)
    {
        switch (reservation.Status)
        {
            case ReservationStatus.Paid:
                return false;
            case ReservationStatus.Released:
                throw new DomainException($"Payout {reservation.PayoutId} was released and cannot be confirmed.");
        }

        var expected = reservation.Items.Select(item => item.CommissionId).ToHashSet();
        var reserved = commissions.Where(commission => expected.Contains(commission.Id)).ToList();
        if (reserved.Count != expected.Count
            || reserved.Any(commission => commission.Status != CommissionStatus.Reserved || commission.PayoutId != reservation.PayoutId))
        {
            throw new DomainException($"Commissions of payout {reservation.PayoutId} are not in the reserved state.");
        }

        foreach (var commission in reserved)
        {
            commission.Status = CommissionStatus.Paid;
            commission.PaidAt = paidAt;
        }

        reservation.Status = ReservationStatus.Paid;
        reservation.CompletedAt = paidAt;
        return true;
    }

    /// <summary>Returns the reserved commissions to "accrued", so a later payout can take them.</summary>
    /// <returns><c>false</c> when the payout was already released.</returns>
    public bool Release(PayoutReservation reservation, IReadOnlyList<Commission> commissions, string reason, DateTimeOffset now)
    {
        switch (reservation.Status)
        {
            case ReservationStatus.Released:
                return false;
            case ReservationStatus.Paid:
                throw new DomainException($"Payout {reservation.PayoutId} is already paid and cannot be released.");
        }

        var expected = reservation.Items.Select(item => item.CommissionId).ToHashSet();
        var reserved = commissions.Where(commission => expected.Contains(commission.Id)).ToList();
        if (reserved.Count != expected.Count
            || reserved.Any(commission => commission.Status != CommissionStatus.Reserved || commission.PayoutId != reservation.PayoutId))
        {
            throw new DomainException($"Commissions of payout {reservation.PayoutId} are not in the reserved state.");
        }

        foreach (var commission in reserved)
        {
            commission.Status = CommissionStatus.Accrued;
            commission.PayoutId = null;
            commission.ReservedAt = null;
        }

        reservation.Status = ReservationStatus.Released;
        reservation.CompletedAt = now;
        reservation.ReleaseReason = reason;
        return true;
    }

    /// <summary>
    /// Called when a release comes for a payout that was never reserved here. The payout is saved as
    /// released, so a late reservation command for it reserves nothing.
    /// </summary>
    public PayoutReservation RegisterReleased(Guid payoutId, string partnerId, string reason, DateTimeOffset now) => new()
    {
        PayoutId = payoutId,
        PartnerId = partnerId,
        Status = ReservationStatus.Released,
        TotalAmount = 0,
        Items = [],
        ReservedAt = now,
        CompletedAt = now,
        ReleaseReason = reason,
    };
}
