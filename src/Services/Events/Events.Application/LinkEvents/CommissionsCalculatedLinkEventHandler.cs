using Microsoft.Extensions.Logging;
using ValetaxTest.Core.Persistence;
using ValetaxTest.EventBus.Abstractions;
using ValetaxTest.Events.Application.Mappings;
using ValetaxTest.Events.Domain.ProfitEvents;

namespace ValetaxTest.Events.Application.LinkEvents;

/// <summary>Stores the calculated commissions of an event for event details.</summary>
public sealed class CommissionsCalculatedLinkEventHandler(
    IProfitEventRepository events,
    ProfitEventService eventService,
    IUnitOfWork unitOfWork,
    EventsMetrics metrics,
    ILogger<CommissionsCalculatedLinkEventHandler> logger) : IMessageHandler<CommissionsCalculatedLinkEvent>
{
    public async Task HandleAsync(CommissionsCalculatedLinkEvent message, MessageContext context, CancellationToken cancellationToken)
    {
        // The event is saved before the message about it is sent, so it must exist here.
        var profitEvent = await events.FindAsync(message.EventId, cancellationToken)
            ?? throw new InvalidOperationException($"Commissions were calculated for unknown event '{message.EventId}'.");

        var commissions = message.Commissions.Select(commission => commission.ToDomain(message.EventId)).ToList();
        var outcome = message.Outcome.ToDomain();

        if (!eventService.CompleteProcessing(profitEvent, outcome, message.SchemaType?.ToDomain(), commissions, message.CalculatedAt))
        {
            logger.LogInformation("Event {EventId} is already processed", message.EventId);
            return;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        metrics.EventProcessed(outcome);
        logger.LogInformation("Event {EventId} processed: {Outcome}, {Count} commission(s)",
            message.EventId, outcome, commissions.Count);
    }
}
