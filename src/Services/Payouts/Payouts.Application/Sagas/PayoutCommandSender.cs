using Microsoft.Extensions.Options;
using ValetaxTest.EventBus.Abstractions;
using ValetaxTest.Payouts.Application.Mappings;
using ValetaxTest.Payouts.Domain.Sagas;

namespace ValetaxTest.Payouts.Application.Sagas;

/// <summary>
/// Adds the command of the saga's current step to the outbox, so it is saved in the same transaction as
/// the new saga state.
/// </summary>
public sealed class PayoutCommandSender(IOutbox outbox, PayoutSagaStateMachine stateMachine, IOptions<PayoutOptions> options)
{
    public PayoutSagaStep SendPendingCommand(PayoutSaga saga)
    {
        var payoutId = saga.Id.ToString();
        var step = stateMachine.PendingStep(saga);

        switch (step)
        {
            case PayoutSagaStep.ReserveCommissions:
                outbox.Enqueue(Topics.CommissionPayoutCommands, payoutId,
                    saga.ToReserveCommissions(options.Value.MaxItemsPerPayout), correlationId: payoutId);
                break;

            case PayoutSagaStep.CreditWallet:
                // Keyed by the wallet owner: commands for one wallet are processed in order.
                outbox.Enqueue(Topics.WalletPayoutCommands, saga.PartnerId, saga.ToCreditWallet(), correlationId: payoutId);
                break;

            case PayoutSagaStep.ConfirmPayment:
                outbox.Enqueue(Topics.CommissionPayoutCommands, payoutId, saga.ToConfirmCommissionsPaid(), correlationId: payoutId);
                break;

            case PayoutSagaStep.ReleaseCommissions:
                outbox.Enqueue(Topics.CommissionPayoutCommands, payoutId, saga.ToReleaseCommissions(), correlationId: payoutId);
                break;
        }

        return step;
    }
}
