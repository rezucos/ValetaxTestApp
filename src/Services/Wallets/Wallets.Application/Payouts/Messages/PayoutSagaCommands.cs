// Each service taking part in the payout saga has its own copy of these messages:
// the class and property names must be the same in all copies.

namespace ValetaxTest.Wallets.Application.Payouts.Messages;

// Commands of the payout saga to Wallets (topic payouts.wallet-commands.v1, key = user id).

/// <summary>Wallets: credit the partner's wallet with the reserved commissions.</summary>
public sealed record CreditWallet(Guid PayoutId, string UserId, IReadOnlyList<PayoutItem> Items);
