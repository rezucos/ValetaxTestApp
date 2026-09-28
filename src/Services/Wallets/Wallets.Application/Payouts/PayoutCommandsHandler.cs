using Microsoft.Extensions.Logging;
using ValetaxTest.Core.Persistence;
using ValetaxTest.EventBus.Abstractions;
using ValetaxTest.Wallets.Application.Mappings;
using ValetaxTest.Wallets.Application.Payouts.Messages;
using ValetaxTest.Wallets.Domain.Wallets;

namespace ValetaxTest.Wallets.Application.Payouts;

/// <summary>Handles payout saga commands sent to Wallets.</summary>
public sealed class PayoutCommandsHandler(
    IWalletRepository wallets,
    IWalletPayoutRepository payouts,
    WalletRules walletRules,
    IOutbox outbox,
    IUnitOfWork unitOfWork,
    WalletsMetrics metrics,
    TimeProvider timeProvider,
    ILogger<PayoutCommandsHandler> logger) : IMessageHandler<CreditWallet>
{
    /// <summary>
    /// Credits the reserved commissions to the partner's wallet. The result (credited or rejected) is stored
    /// by payout id, and a repeated command gets the same reply. A commission can be credited only once.
    /// </summary>
    public async Task HandleAsync(CreditWallet command, MessageContext context, CancellationToken cancellationToken)
    {
        var payout = await payouts.FindAsync(command.PayoutId, cancellationToken);
        if (payout is null)
        {
            var now = timeProvider.GetUtcNow();
            var wallet = await wallets.FindForUpdateAsync(command.UserId, cancellationToken);
            if (wallet is null)
            {
                // The wallet is normally opened on UserRegisteredLinkEvent; if that message is late, the payout does not wait.
                wallet = walletRules.Open(command.UserId, now);
                wallets.Add(wallet);
            }

            var items = command.Items.Select(item => item.ToDomain()).ToList();
            payout = walletRules.Credit(wallet, command.PayoutId, items, now);
            payouts.Add(payout);
            metrics.PayoutProcessed(payout);

            if (payout.Status == WalletPayoutStatus.Credited)
            {
                logger.LogInformation("Payout {PayoutId}: {Amount} credited to the wallet of {UserId}, balance {Balance}",
                    payout.PayoutId, payout.Amount, wallet.UserId, wallet.Balance);
            }
            else
            {
                logger.LogWarning("Payout {PayoutId} to the wallet of {UserId} rejected: {Reason}",
                    payout.PayoutId, wallet.UserId, payout.RejectionReason);
            }
        }
        else
        {
            logger.LogInformation("Payout {PayoutId} is already {Status}, repeating the reply", payout.PayoutId, payout.Status);
        }

        outbox.Enqueue(Topics.PayoutSagaReplies, command.PayoutId.ToString(), payout.ToContract(), correlationId: command.PayoutId.ToString());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
