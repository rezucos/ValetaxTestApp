using ValetaxTest.Events.Application.ProfitEvents;
using ValetaxTest.Events.Domain.ProfitEvents;

namespace ValetaxTest.Events.Application.Mappings;

public static class DomainToDtoMapper
{
    extension(ProfitEvent profitEvent)
    {
        public ProfitEventDto ToDto() => new(
            profitEvent.ExternalId,
            profitEvent.UserId,
            profitEvent.Profit,
            profitEvent.OccurredAt,
            profitEvent.ReceivedAt,
            profitEvent.Status);

        public ProfitEventDetailsDto ToDetailsDto(IReadOnlyList<EventCommissionDto> commissions) => new(
            profitEvent.ExternalId,
            profitEvent.UserId,
            profitEvent.Profit,
            profitEvent.OccurredAt,
            profitEvent.ReceivedAt,
            profitEvent.Status,
            profitEvent.Outcome,
            profitEvent.SchemaType,
            profitEvent.ProcessedAt,
            commissions.Sum(commission => commission.Amount),
            commissions);
    }

    extension(EventCommission commission)
    {
        /// <param name="payment"><c>null</c> while the commission is not paid.</param>
        public EventCommissionDto ToDto(CommissionPayment? payment) => new(
            commission.CommissionId,
            commission.PartnerId,
            commission.Level,
            commission.RatePercent,
            commission.Amount,
            commission.SchemaType,
            payment is not null,
            payment?.PaidAt,
            payment?.PayoutId);
    }
}
