using Microsoft.Extensions.Logging;
using ValetaxTest.Core.Persistence;
using ValetaxTest.EventBus.Abstractions;
using ValetaxTest.Events.Application.Mappings;
using ValetaxTest.Events.Domain.ProfitEvents;

namespace ValetaxTest.Events.Application.LinkEvents;

/// <summary>Records paid commissions, so event details can show whether each commission is paid.</summary>
public sealed class CommissionsPaidLinkEventHandler(
    ICommissionPaymentRepository payments,
    IUnitOfWork unitOfWork,
    ILogger<CommissionsPaidLinkEventHandler> logger) : IMessageHandler<CommissionsPaidLinkEvent>
{
    public async Task HandleAsync(CommissionsPaidLinkEvent message, MessageContext context, CancellationToken cancellationToken)
    {
        var commissionIds = message.Commissions.Select(commission => commission.CommissionId).ToList();
        var recorded = await payments.GetRecordedAsync(commissionIds, cancellationToken);

        var newPayments = message.Commissions
            .Where(commission => !recorded.Contains(commission.CommissionId))
            .Select(commission => commission.ToDomain(message.PayoutId, message.PaidAt))
            .ToList();

        if (newPayments.Count == 0)
        {
            return;
        }

        payments.AddRange(newPayments);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Payout {PayoutId}: {Count} commission(s) of partner {PartnerId} marked as paid",
            message.PayoutId, newPayments.Count, message.PartnerId);
    }
}
