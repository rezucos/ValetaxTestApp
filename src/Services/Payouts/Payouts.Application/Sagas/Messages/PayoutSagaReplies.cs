// Each service taking part in the payout saga has its own copy of these messages:
// the class and property names must be the same in all copies.

namespace ValetaxTest.Payouts.Application.Sagas.Messages;

// Replies to the payout saga (topic payouts.saga-replies.v1, key = payout id).

/// <summary>The commissions were reserved; an empty list means there was nothing to pay out.</summary>
public sealed record CommissionsReserved(Guid PayoutId, string PartnerId, IReadOnlyList<PayoutItem> Items);

/// <summary>The partner's wallet was credited with the payout amount.</summary>
public sealed record WalletCredited(Guid PayoutId, string UserId, decimal Amount, DateTimeOffset CreditedAt);

/// <summary>The wallet refused the payout; the saga then releases the reserved commissions.</summary>
public sealed record WalletCreditRejected(Guid PayoutId, string UserId, string Reason);

/// <summary>The reserved commissions were marked as paid.</summary>
public sealed record CommissionsPaymentConfirmed(Guid PayoutId);

/// <summary>The reserved commissions were returned to "accrued".</summary>
public sealed record CommissionsReleased(Guid PayoutId);
