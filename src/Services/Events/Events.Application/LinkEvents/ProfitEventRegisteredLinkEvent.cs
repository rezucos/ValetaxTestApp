// Each service that sends or receives this message has its own copy of this file:
// the class and property names must be the same in all copies.

namespace ValetaxTest.Events.Application.LinkEvents;

/// <summary>A profit or loss event was accepted. Commissions calculates commissions for it.</summary>
public sealed record ProfitEventRegisteredLinkEvent(
    string EventId,
    string UserId,
    decimal Profit,
    DateTimeOffset OccurredAt,
    DateTimeOffset ReceivedAt);
