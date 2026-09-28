using ValetaxTest.Commissions.Application.LinkEvents;
using ValetaxTest.Commissions.Application.Payouts.Messages;
using ValetaxTest.Commissions.Domain.Accrual;
using ValetaxTest.Commissions.Domain.Payouts;
using CommissionSchemaType = ValetaxTest.Commissions.Domain.Schemas.CommissionSchemaType;
using ContractSchemaType = ValetaxTest.Commissions.Application.LinkEvents.CommissionSchemaType;

namespace ValetaxTest.Commissions.Application.Mappings;

/// <summary>Builds messages sent by the service. Messages have their own enums, so enums are converted too.</summary>
public static class DomainToContractMapper
{
    extension(AccrualResult result)
    {
        public CommissionsCalculatedLinkEvent ToContract() => new(
            result.ProcessedEvent.EventId,
            result.ProcessedEvent.UserId,
            result.ProcessedEvent.Outcome.ToContract(),
            result.ProcessedEvent.SchemaType?.ToContract(),
            result.Commissions.Select(commission => commission.ToContract()).ToList(),
            result.ProcessedEvent.ProcessedAt);
    }

    extension(Commission commission)
    {
        public CalculatedCommission ToContract() => new(
            commission.Id,
            commission.PartnerId,
            commission.Level,
            commission.RatePercent,
            commission.Amount,
            commission.SchemaType.ToContract());
    }

    extension(PayoutReservation reservation)
    {
        public CommissionsReserved ToCommissionsReserved() => new(
            reservation.PayoutId,
            reservation.PartnerId,
            reservation.Items.Select(item => item.ToPayoutItem()).ToList());

        public CommissionsPaidLinkEvent ToCommissionsPaid(DateTimeOffset paidAt) => new(
            reservation.PayoutId,
            reservation.PartnerId,
            paidAt,
            reservation.Items.Select(item => item.ToPaidCommission()).ToList());
    }

    extension(ReservedCommission item)
    {
        public PayoutItem ToPayoutItem() => new(item.CommissionId, item.EventId, item.Level, item.Amount, item.SchemaType.ToContract());

        public PaidCommission ToPaidCommission() => new(item.CommissionId, item.EventId, item.Amount);
    }

    extension(ProcessingOutcome outcome)
    {
        public CommissionCalculationOutcome ToContract() => outcome switch
        {
            ProcessingOutcome.Accrued => CommissionCalculationOutcome.Accrued,
            ProcessingOutcome.NotEligible => CommissionCalculationOutcome.NotEligible,
            ProcessingOutcome.NoPartners => CommissionCalculationOutcome.NoPartners,
            ProcessingOutcome.UnknownUser => CommissionCalculationOutcome.UnknownUser,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unknown processing outcome."),
        };
    }

    extension(CommissionSchemaType schemaType)
    {
        public ContractSchemaType ToContract() => schemaType switch
        {
            CommissionSchemaType.Linear => ContractSchemaType.Linear,
            CommissionSchemaType.Fibonacci => ContractSchemaType.Fibonacci,
            _ => throw new ArgumentOutOfRangeException(nameof(schemaType), schemaType, "Unknown commission schema."),
        };
    }
}
