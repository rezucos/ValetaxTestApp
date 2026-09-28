namespace ValetaxTest.Events.Domain.ProfitEvents;

public interface IProfitEventRepository
{
    /// <summary>Loads the event to change it.</summary>
    Task<ProfitEvent?> FindAsync(string externalId, CancellationToken cancellationToken);

    /// <summary>Reads the stored event without tracking changes.</summary>
    Task<ProfitEvent?> GetSnapshotAsync(string externalId, CancellationToken cancellationToken);

    void Add(ProfitEvent profitEvent);
}

public interface ICommissionPaymentRepository
{
    /// <summary>Returns the ids of the given commissions whose payment is already recorded.</summary>
    Task<IReadOnlySet<Guid>> GetRecordedAsync(IReadOnlyCollection<Guid> commissionIds, CancellationToken cancellationToken);

    void AddRange(IEnumerable<CommissionPayment> payments);
}
