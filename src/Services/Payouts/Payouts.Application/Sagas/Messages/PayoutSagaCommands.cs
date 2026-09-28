// Each service taking part in the payout saga has its own copy of these messages:
// the class and property names must be the same in all copies.

namespace ValetaxTest.Payouts.Application.Sagas.Messages;

// Commands of the payout saga. A command sent again after a timeout gives the same result:
// Commissions and Wallets recognize it by PayoutId.

/// <summary>Commissions: reserve up to <see cref="MaxItems"/> accrued commissions of the partner for the payout.</summary>
public sealed record ReserveCommissions(Guid PayoutId, string PartnerId, int MaxItems);

/// <summary>Wallets: credit the partner's wallet with the reserved commissions.</summary>
public sealed record CreditWallet(Guid PayoutId, string UserId, IReadOnlyList<PayoutItem> Items);

/// <summary>Commissions: the wallet was credited, mark the reserved commissions as paid.</summary>
public sealed record ConfirmCommissionsPaid(Guid PayoutId, string PartnerId, DateTimeOffset PaidAt);

/// <summary>Commissions: the wallet rejected the payout, return the reserved commissions to "accrued".</summary>
public sealed record ReleaseCommissions(Guid PayoutId, string PartnerId, string Reason);
