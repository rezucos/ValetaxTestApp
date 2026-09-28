using ValetaxTest.Payouts.Application.Sagas.Messages;
using ValetaxTest.Payouts.Domain.Sagas;
using CommissionSchemaType = ValetaxTest.Payouts.Domain.Sagas.CommissionSchemaType;
using ContractSchemaType = ValetaxTest.Payouts.Application.Sagas.Messages.CommissionSchemaType;

namespace ValetaxTest.Payouts.Application.Mappings;

/// <summary>Builds the saga commands. Messages have their own enums, so enums are converted too.</summary>
public static class DomainToContractMapper
{
    extension(PayoutSaga saga)
    {
        public ReserveCommissions ToReserveCommissions(int maxItems) => new(saga.Id, saga.PartnerId, maxItems);

        public CreditWallet ToCreditWallet() => new(saga.Id, saga.PartnerId, saga.Items.Select(item => item.ToContract()).ToList());

        public ConfirmCommissionsPaid ToConfirmCommissionsPaid() => new(saga.Id, saga.PartnerId, saga.PaidAt ?? saga.UpdatedAt);

        public ReleaseCommissions ToReleaseCommissions() => new(saga.Id, saga.PartnerId, saga.FailureReason ?? "Payout rejected");
    }

    extension(PayoutSagaItem item)
    {
        public PayoutItem ToContract() => new(item.CommissionId, item.EventId, item.Level, item.Amount, item.SchemaType.ToContract());
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
