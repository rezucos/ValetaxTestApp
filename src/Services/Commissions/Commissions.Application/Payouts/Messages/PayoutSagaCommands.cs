// Each service taking part in the payout saga has its own copy of these messages:
// the class and property names must be the same in all copies.

namespace ValetaxTest.Commissions.Application.Payouts.Messages;

// Commands of the payout saga to Commissions (topic payouts.commission-commands.v1, key = payout id).

/// <summary>Commissions: reserve up to <see cref="MaxItems"/> accrued commissions of the partner for the payout.</summary>
public sealed record ReserveCommissions(Guid PayoutId, string PartnerId, int MaxItems);

/// <summary>Commissions: the wallet was credited, mark the reserved commissions as paid.</summary>
public sealed record ConfirmCommissionsPaid(Guid PayoutId, string PartnerId, DateTimeOffset PaidAt);

/// <summary>Commissions: the wallet rejected the payout, return the reserved commissions to "accrued".</summary>
public sealed record ReleaseCommissions(Guid PayoutId, string PartnerId, string Reason);
