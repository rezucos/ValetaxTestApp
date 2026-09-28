using Microsoft.EntityFrameworkCore;
using ValetaxTest.Payouts.Domain.Sagas;

namespace ValetaxTest.Payouts.Infrastructure.Persistence.Repositories;

public sealed class PayoutSagaRepository(PayoutsDbContext dbContext) : IPayoutSagaRepository
{
    private static readonly string[] ActiveStates =
        PayoutSagaStateMachine.ActiveStates.Select(state => state.ToString()).ToArray();

    public async Task<PayoutSaga?> FindForUpdateAsync(Guid payoutId, CancellationToken cancellationToken)
    {
        var rows = await dbContext.Sagas
            .FromSql($"SELECT * FROM payout_sagas WHERE id = {payoutId} FOR UPDATE")
            .ToListAsync(cancellationToken);
        return rows.SingleOrDefault();
    }

    public async Task<IReadOnlyList<PayoutSaga>> LockTimedOutAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken) =>
        await dbContext.Sagas
            .FromSql($"""
                SELECT * FROM payout_sagas
                WHERE state = ANY({ActiveStates}) AND next_attempt_at <= {now}
                ORDER BY next_attempt_at
                LIMIT {limit}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlySet<string>> GetPartnersWithActiveSagasAsync(IReadOnlyCollection<string> partnerIds, CancellationToken cancellationToken)
    {
        if (partnerIds.Count == 0)
        {
            return new HashSet<string>();
        }

        var busy = await dbContext.Sagas
            .Where(saga => PayoutSagaStateMachine.ActiveStates.Contains(saga.State) && partnerIds.Contains(saga.PartnerId))
            .Select(saga => saga.PartnerId)
            .ToListAsync(cancellationToken);

        return busy.ToHashSet(StringComparer.Ordinal);
    }

    public void Add(PayoutSaga saga) => dbContext.Sagas.Add(saga);
}
