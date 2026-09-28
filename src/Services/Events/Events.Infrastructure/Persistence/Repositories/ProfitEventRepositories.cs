using Microsoft.EntityFrameworkCore;
using ValetaxTest.Events.Domain.ProfitEvents;

namespace ValetaxTest.Events.Infrastructure.Persistence.Repositories;

public sealed class ProfitEventRepository(EventsDbContext dbContext) : IProfitEventRepository
{
    public Task<ProfitEvent?> FindAsync(string externalId, CancellationToken cancellationToken) =>
        dbContext.ProfitEvents.SingleOrDefaultAsync(profitEvent => profitEvent.ExternalId == externalId, cancellationToken);

    public Task<ProfitEvent?> GetSnapshotAsync(string externalId, CancellationToken cancellationToken) =>
        dbContext.ProfitEvents.AsNoTracking()
            .SingleOrDefaultAsync(profitEvent => profitEvent.ExternalId == externalId, cancellationToken);

    public void Add(ProfitEvent profitEvent) => dbContext.ProfitEvents.Add(profitEvent);
}

public sealed class CommissionPaymentRepository(EventsDbContext dbContext) : ICommissionPaymentRepository
{
    public async Task<IReadOnlySet<Guid>> GetRecordedAsync(IReadOnlyCollection<Guid> commissionIds, CancellationToken cancellationToken)
    {
        var recorded = await dbContext.CommissionPayments
            .Where(payment => commissionIds.Contains(payment.CommissionId))
            .Select(payment => payment.CommissionId)
            .ToListAsync(cancellationToken);

        return recorded.ToHashSet();
    }

    public void AddRange(IEnumerable<CommissionPayment> payments) => dbContext.CommissionPayments.AddRange(payments);
}
