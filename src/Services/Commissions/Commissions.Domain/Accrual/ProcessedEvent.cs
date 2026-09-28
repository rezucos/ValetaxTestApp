using ValetaxTest.Commissions.Domain.Schemas;

namespace ValetaxTest.Commissions.Domain.Accrual;

/// <summary>
/// A processed profit event: keeps the event from being accrued twice and stores the result of
/// the calculation.
/// </summary>
public sealed class ProcessedEvent
{
    public required string EventId { get; set; }

    public required string UserId { get; set; }

    public decimal Profit { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public ProcessingOutcome Outcome { get; set; }

    /// <summary>Schema applied; set only when commissions were accrued.</summary>
    public CommissionSchemaType? SchemaType { get; set; }

    public int CommissionsCount { get; set; }

    public decimal CommissionsTotal { get; set; }

    public DateTimeOffset ProcessedAt { get; set; }
}

public enum ProcessingOutcome
{
    /// <summary>Commissions were accrued to the partners above the event owner.</summary>
    Accrued = 1,

    /// <summary>Profit is not positive (or too small to produce a commission).</summary>
    NotEligible = 2,

    /// <summary>The event owner has no partners.</summary>
    NoPartners = 3,

    /// <summary>The event owner is not registered in the partner tree.</summary>
    UnknownUser = 4,
}

/// <summary>Profit event to be accrued.</summary>
public sealed record AccrualRequest(string EventId, string UserId, decimal Profit, DateTimeOffset OccurredAt);

public interface IProcessedEventRepository
{
    Task<bool> ExistsAsync(string eventId, CancellationToken cancellationToken);

    void Add(ProcessedEvent processedEvent);
}

public interface ICommissionRepository
{
    void AddRange(IEnumerable<Commission> commissions);

    /// <summary>
    /// Locks up to <paramref name="limit"/> accrued commissions of the partner (oldest first) until the
    /// transaction ends; rows locked by other transactions are skipped.
    /// </summary>
    Task<IReadOnlyList<Commission>> LockAccruedAsync(string partnerId, int limit, CancellationToken cancellationToken);

    Task<IReadOnlyList<Commission>> GetByPayoutAsync(Guid payoutId, CancellationToken cancellationToken);
}
