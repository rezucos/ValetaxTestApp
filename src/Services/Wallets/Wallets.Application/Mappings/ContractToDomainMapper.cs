using ValetaxTest.Wallets.Application.Payouts.Messages;
using ValetaxTest.Wallets.Domain.Wallets;
using CommissionSchemaType = ValetaxTest.Wallets.Domain.Wallets.CommissionSchemaType;
using ContractSchemaType = ValetaxTest.Wallets.Application.Payouts.Messages.CommissionSchemaType;

namespace ValetaxTest.Wallets.Application.Mappings;

/// <summary>Converts received messages to domain objects. Messages have their own enums, so enums are converted too.</summary>
public static class ContractToDomainMapper
{
    extension(PayoutItem item)
    {
        public WalletPayoutItem ToDomain() => new()
        {
            CommissionId = item.CommissionId,
            EventId = item.EventId,
            Level = item.Level,
            Amount = item.Amount,
            SchemaType = item.SchemaType.ToDomain(),
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
