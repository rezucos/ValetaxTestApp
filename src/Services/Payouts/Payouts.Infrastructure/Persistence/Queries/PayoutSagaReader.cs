using Microsoft.EntityFrameworkCore;
using ValetaxTest.Payouts.Application.Abstractions;
using ValetaxTest.Payouts.Application.Mappings;
using ValetaxTest.Payouts.Application.Sagas;
using ValetaxTest.Payouts.Domain.Sagas;

namespace ValetaxTest.Payouts.Infrastructure.Persistence.Queries;

public sealed class PayoutSagaReader(PayoutsDbContext dbContext) : IPayoutSagaReader
{
    public async Task<IReadOnlyList<PayoutSummaryDto>> ListAsync(
        string? partnerId,
        PayoutSagaState? state,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var sagas = dbContext.Sagas.AsNoTracking();
        if (partnerId is not null)
        {
            sagas = sagas.Where(saga => saga.PartnerId == partnerId);
        }

        if (state is not null)
        {
            sagas = sagas.Where(saga => saga.State == state);
        }

        var rows = await sagas
            .OrderByDescending(saga => saga.CreatedAt).ThenBy(saga => saga.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);

        return rows.Select(saga => saga.ToSummaryDto()).ToList();
    }

    public async Task<PayoutDetailsDto?> GetAsync(Guid payoutId, CancellationToken cancellationToken)
    {
        var saga = await dbContext.Sagas.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == payoutId, cancellationToken);
        return saga?.ToDetailsDto();
    }
}
