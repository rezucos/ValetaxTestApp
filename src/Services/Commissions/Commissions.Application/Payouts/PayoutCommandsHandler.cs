using Microsoft.Extensions.Logging;
using ValetaxTest.Commissions.Application.Mappings;
using ValetaxTest.Commissions.Application.Payouts.Messages;
using ValetaxTest.Commissions.Domain.Accrual;
using ValetaxTest.Commissions.Domain.Payouts;
using ValetaxTest.Core.Persistence;
using ValetaxTest.EventBus.Abstractions;

namespace ValetaxTest.Commissions.Application.Payouts;

/// <summary>
/// Handles payout saga commands sent to Commissions. The reservation stores the result of each payout,
/// so a command sent again after a timeout gets the same reply.
/// </summary>
public sealed class PayoutCommandsHandler(
    IPayoutReservationRepository reservations,
    ICommissionRepository commissions,
    PayoutReservationService reservationService,
    IOutbox outbox,
    IUnitOfWork unitOfWork,
    CommissionsMetrics metrics,
    TimeProvider timeProvider,
    ILogger<PayoutCommandsHandler> logger) :
    IMessageHandler<ReserveCommissions>,
    IMessageHandler<ConfirmCommissionsPaid>,
    IMessageHandler<ReleaseCommissions>
{
    private const int MaxItemsLimit = 5000;

    /// <summary>Reserves accrued commissions of the partner for the payout.</summary>
    public async Task HandleAsync(ReserveCommissions command, MessageContext context, CancellationToken cancellationToken)
    {
        // Payouts checks this limit at startup (Payouts:MaxItemsPerPayout), so a value out of range is a bug.
        if (command.MaxItems is < 1 or > MaxItemsLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(command), command.MaxItems, $"MaxItems must be within 1..{MaxItemsLimit}.");
        }

        var reservation = await reservations.FindForUpdateAsync(command.PayoutId, cancellationToken);
        if (reservation is null)
        {
            var accrued = await commissions.LockAccruedAsync(command.PartnerId, command.MaxItems, cancellationToken);

            reservation = reservationService.Reserve(command.PayoutId, command.PartnerId, accrued, timeProvider.GetUtcNow());
            reservations.Add(reservation);

            logger.LogInformation("Payout {PayoutId}: reserved {Count} commission(s) of partner {PartnerId} for {Amount}",
                command.PayoutId, reservation.Items.Count, command.PartnerId, reservation.TotalAmount);
        }
        else
        {
            logger.LogInformation("Payout {PayoutId}: reservation already exists ({Status}), repeating the reply",
                command.PayoutId, reservation.Status);
        }

        outbox.Enqueue(
            Topics.PayoutSagaReplies,
            command.PayoutId.ToString(),
            reservation.ToCommissionsReserved(),
            correlationId: command.PayoutId.ToString());

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>The wallet was credited: marks the reserved commissions as paid.</summary>
    public async Task HandleAsync(ConfirmCommissionsPaid command, MessageContext context, CancellationToken cancellationToken)
    {
        // Payouts confirms only reserved payouts, so a missing reservation is a bug.
        var reservation = await reservations.FindForUpdateAsync(command.PayoutId, cancellationToken)
            ?? throw new InvalidOperationException($"Payout {command.PayoutId} has no reservation to confirm.");

        var reserved = reservation.Status == ReservationStatus.Reserved
            ? await commissions.GetByPayoutAsync(command.PayoutId, cancellationToken)
            : [];

        if (reservationService.ConfirmPaid(reservation, reserved, command.PaidAt))
        {
            // Lets Events show these commissions as paid.
            outbox.Enqueue(
                Topics.CommissionsPaid,
                command.PayoutId.ToString(),
                reservation.ToCommissionsPaid(command.PaidAt),
                correlationId: command.PayoutId.ToString());

            metrics.Paid(reservation.Items.Count);
            logger.LogInformation("Payout {PayoutId}: {Count} commission(s) of partner {PartnerId} marked as paid",
                command.PayoutId, reservation.Items.Count, reservation.PartnerId);
        }

        outbox.Enqueue(
            Topics.PayoutSagaReplies,
            command.PayoutId.ToString(),
            new CommissionsPaymentConfirmed(command.PayoutId),
            correlationId: command.PayoutId.ToString());

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>The wallet rejected the payout: returns the reserved commissions to "accrued".</summary>
    public async Task HandleAsync(ReleaseCommissions command, MessageContext context, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var reservation = await reservations.FindForUpdateAsync(command.PayoutId, cancellationToken);

        if (reservation is null)
        {
            reservations.Add(reservationService.RegisterReleased(command.PayoutId, command.PartnerId, command.Reason, now));
            logger.LogWarning("Payout {PayoutId}: release arrived before the reservation, the payout is marked as released", command.PayoutId);
        }
        else
        {
            var reserved = reservation.Status == ReservationStatus.Reserved
                ? await commissions.GetByPayoutAsync(command.PayoutId, cancellationToken)
                : [];

            if (reservationService.Release(reservation, reserved, command.Reason, now))
            {
                metrics.Released();
                logger.LogWarning("Payout {PayoutId}: {Count} commission(s) of partner {PartnerId} released: {Reason}",
                    command.PayoutId, reservation.Items.Count, reservation.PartnerId, command.Reason);
            }
        }

        outbox.Enqueue(
            Topics.PayoutSagaReplies,
            command.PayoutId.ToString(),
            new CommissionsReleased(command.PayoutId),
            correlationId: command.PayoutId.ToString());

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
