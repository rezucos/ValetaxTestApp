// Each service that sends or receives this message has its own copy of this file:
// the class and property names must be the same in all copies.

namespace ValetaxTest.Events.Application.LinkEvents;

/// <summary>Commissions were paid to a partner's wallet. Events marks them as paid in event details.</summary>
public sealed record CommissionsPaidLinkEvent(
    Guid PayoutId,
    string PartnerId,
    DateTimeOffset PaidAt,
    IReadOnlyList<PaidCommission> Commissions);

public sealed record PaidCommission(Guid CommissionId, string EventId, decimal Amount);
