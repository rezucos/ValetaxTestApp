using ValetaxTest.Commissions.Application.LinkEvents;
using ValetaxTest.Commissions.Domain.Accrual;

namespace ValetaxTest.Commissions.Application.Mappings;

/// <summary>Converts received messages to domain objects.</summary>
public static class ContractToDomainMapper
{
    extension(ProfitEventRegisteredLinkEvent message)
    {
        public AccrualRequest ToDomain() => new(message.EventId, message.UserId, message.Profit, message.OccurredAt);
    }
}
