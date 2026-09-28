using ValetaxTest.Payouts.Application.Sagas.Messages;
using ValetaxTest.Payouts.Domain.Sagas;
using CommissionSchemaType = ValetaxTest.Payouts.Domain.Sagas.CommissionSchemaType;
using ContractSchemaType = ValetaxTest.Payouts.Application.Sagas.Messages.CommissionSchemaType;

namespace ValetaxTest.Payouts.Application.Mappings;

/// <summary>Converts received messages to domain objects. Messages have their own enums, so enums are converted too.</summary>
public static class ContractToDomainMapper
{
    extension(PayoutItem item)
    {
        public PayoutSagaItem ToDomain() => new(item.CommissionId, item.EventId, item.Level, item.Amount, item.SchemaType.ToDomain());
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
