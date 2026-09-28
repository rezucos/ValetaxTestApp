using Microsoft.Extensions.Logging;
using ValetaxTest.Core.Persistence;
using ValetaxTest.EventBus.Abstractions;
using ValetaxTest.Payouts.Application.Mappings;
using ValetaxTest.Payouts.Application.Sagas.Messages;
using ValetaxTest.Payouts.Domain.Sagas;

namespace ValetaxTest.Payouts.Application.Sagas;

/// <summary>
/// Applies replies of Commissions and Wallets to the saga and sends the command of the next step.
/// The saga row is locked meanwhile, so a reply and a timeout re-send cannot run at the same time.
/// </summary>
public sealed class PayoutSagaRepliesHandler(
    IPayoutSagaRepository sagas,
    PayoutSagaStateMachine stateMachine,
    PayoutCommandSender commandSender,
    IUnitOfWork unitOfWork,
    PayoutsMetrics metrics,
    TimeProvider timeProvider,
    ILogger<PayoutSagaRepliesHandler> logger) :
    IMessageHandler<CommissionsReserved>,
    IMessageHandler<WalletCredited>,
    IMessageHandler<WalletCreditRejected>,
    IMessageHandler<CommissionsPaymentConfirmed>,
    IMessageHandler<CommissionsReleased>
{
    public Task HandleAsync(CommissionsReserved message, MessageContext context, CancellationToken cancellationToken) =>
        ApplyAsync(message.PayoutId, context, (saga, now) =>
            stateMachine.OnCommissionsReserved(saga, message.Items.Select(item => item.ToDomain()).ToList(), now), cancellationToken);

    public Task HandleAsync(WalletCredited message, MessageContext context, CancellationToken cancellationToken) =>
        ApplyAsync(message.PayoutId, context, (saga, now) =>
            stateMachine.OnWalletCredited(saga, message.CreditedAt, now), cancellationToken);

    public Task HandleAsync(WalletCreditRejected message, MessageContext context, CancellationToken cancellationToken) =>
        ApplyAsync(message.PayoutId, context, (saga, now) =>
            stateMachine.OnWalletCreditRejected(saga, message.Reason, now), cancellationToken);

    public Task HandleAsync(CommissionsPaymentConfirmed message, MessageContext context, CancellationToken cancellationToken) =>
        ApplyAsync(message.PayoutId, context, (saga, now) =>
            stateMachine.OnPaymentConfirmed(saga, now), cancellationToken);

    public Task HandleAsync(CommissionsReleased message, MessageContext context, CancellationToken cancellationToken) =>
        ApplyAsync(message.PayoutId, context, (saga, now) =>
            stateMachine.OnCommissionsReleased(saga, now), cancellationToken);

    private async Task ApplyAsync(
        Guid payoutId,
        MessageContext context,
        Func<PayoutSaga, DateTimeOffset, bool> apply,
        CancellationToken cancellationToken)
    {
        var saga = await sagas.FindForUpdateAsync(payoutId, cancellationToken);
        if (saga is null)
        {
            logger.LogWarning("{Reply} for unknown payout {PayoutId} ignored", context.MessageType, payoutId);
            return;
        }

        var previousState = saga.State;
        if (!apply(saga, timeProvider.GetUtcNow()))
        {
            // A repeated or late reply (e.g. to a command sent again after a timeout): the saga has already moved on.
            logger.LogInformation("{Reply} ignored: payout {PayoutId} is already {State}", context.MessageType, payoutId, saga.State);
            return;
        }

        commandSender.SendPendingCommand(saga);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        metrics.Transitioned(saga);
        logger.LogInformation("Payout {PayoutId} of partner {PartnerId}: {From} -> {To}",
            saga.Id, saga.PartnerId, previousState, saga.State);
    }
}
