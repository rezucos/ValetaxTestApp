using ValetaxTest.Events.Application.LinkEvents;
using ValetaxTest.Events.Domain.ProfitEvents;

namespace ValetaxTest.Events.Application.Mappings;

/// <summary>Builds messages sent by the service.</summary>
public static class DomainToContractMapper
{
    extension(ProfitEvent profitEvent)
    {
        public ProfitEventRegisteredLinkEvent ToContract() => new(
            profitEvent.ExternalId,
            profitEvent.UserId,
            profitEvent.Profit,
            profitEvent.OccurredAt,
            profitEvent.ReceivedAt);
    }
}
