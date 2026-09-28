using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ValetaxTest.Commissions.Application.Abstractions;
using ValetaxTest.Commissions.Application.Mappings;
using ValetaxTest.Commissions.Domain.Accrual;
using ValetaxTest.Commissions.Domain.Schemas;
using ValetaxTest.Core.Persistence;
using ValetaxTest.EventBus.Abstractions;

namespace ValetaxTest.Commissions.Application.LinkEvents;

/// <summary>Accrues commissions for a profit event using the schema that is active at that moment.</summary>
public sealed class ProfitEventRegisteredLinkEventHandler(
    IProcessedEventRepository processedEvents,
    ICommissionRepository commissions,
    ICommissionSettingsRepository settings,
    IPartnerHierarchy partnerHierarchy,
    CommissionAccrualService accrualService,
    CommissionSchemaResolver schemaResolver,
    IOutbox outbox,
    IUnitOfWork unitOfWork,
    IOptions<CommissionOptions> options,
    CommissionsMetrics metrics,
    TimeProvider timeProvider,
    ILogger<ProfitEventRegisteredLinkEventHandler> logger) : IMessageHandler<ProfitEventRegisteredLinkEvent>
{
    public async Task HandleAsync(ProfitEventRegisteredLinkEvent message, MessageContext context, CancellationToken cancellationToken)
    {
        // The inbox skips only the same Kafka message; this check also catches the same event sent in another
        // message. The unique (event_id, level) index of commissions is the last safeguard.
        if (await processedEvents.ExistsAsync(message.EventId, cancellationToken))
        {
            logger.LogInformation("Event {EventId} is already processed, skipping", message.EventId);
            return;
        }

        var maxLevels = options.Value.MaxLevels;
        var schema = schemaResolver.Resolve(await settings.GetActiveSchemaAsync(cancellationToken));

        // A loss or zero profit gives no commissions, so the partner chain is not needed.
        var upline = message.Profit > 0
            ? await partnerHierarchy.GetUplineAsync(message.UserId, maxLevels, cancellationToken)
            : [];

        var result = accrualService.Accrue(message.ToDomain(), upline, schema, maxLevels, timeProvider.GetUtcNow());

        processedEvents.Add(result.ProcessedEvent);
        commissions.AddRange(result.Commissions);
        outbox.Enqueue(Topics.CommissionsCalculated, message.EventId, result.ToContract(), correlationId: message.EventId);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        metrics.Accrued(result);
        var processed = result.ProcessedEvent;
        logger.LogInformation(
            "Event {EventId} of user {UserId} processed: {Outcome}, {Count} commission(s) for {Total} using {Schema} schema",
            processed.EventId, processed.UserId, processed.Outcome, processed.CommissionsCount, processed.CommissionsTotal, schema.Type);
    }
}
