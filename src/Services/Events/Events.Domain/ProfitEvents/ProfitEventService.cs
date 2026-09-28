using ValetaxTest.Core;

namespace ValetaxTest.Events.Domain.ProfitEvents;

/// <summary>Rules of profit events.</summary>
public sealed class ProfitEventService
{
    public ProfitEvent Register(string externalId, string userId, decimal profit, DateTimeOffset? occurredAt, DateTimeOffset now) => new()
    {
        ExternalId = externalId,
        UserId = userId,
        Profit = profit,
        OccurredAt = Timestamps.Normalize(occurredAt ?? now),
        ReceivedAt = Timestamps.Normalize(now),
        Status = ProfitEventStatus.Pending,
    };

    /// <summary>
    /// Whether a request with a known ExternalId repeats the stored event (it gets the stored event back)
    /// or brings different data (a conflict). A request without OccurredAt matches any time.
    /// </summary>
    public bool IsSameEvent(ProfitEvent existing, string userId, decimal profit, DateTimeOffset? occurredAt) =>
        string.Equals(existing.UserId, userId, StringComparison.Ordinal)
        && existing.Profit == profit
        && (occurredAt is null || existing.OccurredAt == Timestamps.Normalize(occurredAt.Value));

    /// <summary>Records the result of the commission calculation.</summary>
    /// <returns><c>false</c> when the event is already processed (the same result came again).</returns>
    public bool CompleteProcessing(
        ProfitEvent profitEvent,
        ProcessingOutcome outcome,
        CommissionSchemaType? schemaType,
        IReadOnlyCollection<EventCommission> commissions,
        DateTimeOffset processedAt)
    {
        if (profitEvent.Status == ProfitEventStatus.Processed)
        {
            return false;
        }

        if (commissions.Any(commission => commission.EventId != profitEvent.ExternalId))
        {
            throw new DomainException($"Commissions of another event cannot be attached to event '{profitEvent.ExternalId}'.");
        }

        profitEvent.Status = ProfitEventStatus.Processed;
        profitEvent.Outcome = outcome;
        profitEvent.SchemaType = schemaType;
        profitEvent.ProcessedAt = processedAt;
        profitEvent.Commissions.AddRange(commissions);
        return true;
    }
}
