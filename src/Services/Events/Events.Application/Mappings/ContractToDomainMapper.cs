using ValetaxTest.Events.Application.LinkEvents;
using ValetaxTest.Events.Domain.ProfitEvents;
using CommissionSchemaType = ValetaxTest.Events.Domain.ProfitEvents.CommissionSchemaType;
using ContractSchemaType = ValetaxTest.Events.Application.LinkEvents.CommissionSchemaType;

namespace ValetaxTest.Events.Application.Mappings;

/// <summary>Converts received messages to domain objects. Messages have their own enums, so enums are converted too.</summary>
public static class ContractToDomainMapper
{
    extension(CalculatedCommission commission)
    {
        public EventCommission ToDomain(string eventId) => new()
        {
            CommissionId = commission.CommissionId,
            EventId = eventId,
            PartnerId = commission.PartnerId,
            Level = commission.Level,
            RatePercent = commission.RatePercent,
            Amount = commission.Amount,
            SchemaType = commission.SchemaType.ToDomain(),
        };
    }

    extension(PaidCommission commission)
    {
        public CommissionPayment ToDomain(Guid payoutId, DateTimeOffset paidAt) => new()
        {
            CommissionId = commission.CommissionId,
            PayoutId = payoutId,
            PaidAt = paidAt,
        };
    }

    extension(CommissionCalculationOutcome outcome)
    {
        public ProcessingOutcome ToDomain() => outcome switch
        {
            CommissionCalculationOutcome.Accrued => ProcessingOutcome.CommissionsAccrued,
            CommissionCalculationOutcome.NotEligible => ProcessingOutcome.NotEligible,
            CommissionCalculationOutcome.NoPartners => ProcessingOutcome.NoPartners,
            CommissionCalculationOutcome.UnknownUser => ProcessingOutcome.UnknownUser,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unknown calculation outcome."),
        };
    }

    extension(ContractSchemaType schemaType)
    {
        public CommissionSchemaType ToDomain() => schemaType switch
        {
            ContractSchemaType.Linear => CommissionSchemaType.Linear,
            ContractSchemaType.Fibonacci => CommissionSchemaType.Fibonacci,
            _ => throw new ArgumentOutOfRangeException(nameof(schemaType), schemaType, "Unknown commission schema."),
        };
    }
}
