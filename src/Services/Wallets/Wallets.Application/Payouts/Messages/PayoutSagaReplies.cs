// Each service taking part in the payout saga has its own copy of these messages:
// the class and property names must be the same in all copies.

namespace ValetaxTest.Wallets.Application.Payouts.Messages;

// Replies to the payout saga (topic payouts.saga-replies.v1, key = payout id).

/// <summary>The partner's wallet was credited with the payout amount.</summary>
public sealed record WalletCredited(Guid PayoutId, string UserId, decimal Amount, DateTimeOffset CreditedAt);

/// <summary>The wallet refused the payout; the saga then releases the reserved commissions.</summary>
public sealed record WalletCreditRejected(Guid PayoutId, string UserId, string Reason);
