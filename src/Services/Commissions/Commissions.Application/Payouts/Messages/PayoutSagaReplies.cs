// Each service taking part in the payout saga has its own copy of these messages:
// the class and property names must be the same in all copies.

namespace ValetaxTest.Commissions.Application.Payouts.Messages;

// Replies to the payout saga (topic payouts.saga-replies.v1, key = payout id).

/// <summary>The commissions were reserved; an empty list means there was nothing to pay out.</summary>
public sealed record CommissionsReserved(Guid PayoutId, string PartnerId, IReadOnlyList<PayoutItem> Items);

/// <summary>The reserved commissions were marked as paid.</summary>
public sealed record CommissionsPaymentConfirmed(Guid PayoutId);

/// <summary>The reserved commissions were returned to "accrued".</summary>
public sealed record CommissionsReleased(Guid PayoutId);
