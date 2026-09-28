using Microsoft.EntityFrameworkCore;
using ValetaxTest.Commissions.Domain.Accrual;
using ValetaxTest.Commissions.Domain.Payouts;
using ValetaxTest.Commissions.Domain.Schemas;

namespace ValetaxTest.Commissions.Infrastructure.Persistence.Repositories;

public sealed class CommissionRepository(CommissionsDbContext dbContext) : ICommissionRepository
{
    public void AddRange(IEnumerable<Commission> commissions) => dbContext.Commissions.AddRange(commissions);

    public async Task<IReadOnlyList<Commission>> LockAccruedAsync(string partnerId, int limit, CancellationToken cancellationToken)
    {
        var accrued = nameof(CommissionStatus.Accrued);

        // SKIP LOCKED: commissions locked by another transaction are skipped, not waited for.
        return await dbContext.Commissions
            .FromSql($"""
                SELECT * FROM commissions
                WHERE partner_id = {partnerId} AND status = {accrued}
                ORDER BY accrued_at, id
                LIMIT {limit}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Commission>> GetByPayoutAsync(Guid payoutId, CancellationToken cancellationToken) =>
        await dbContext.Commissions.Where(commission => commission.PayoutId == payoutId).ToListAsync(cancellationToken);
}

public sealed class ProcessedEventRepository(CommissionsDbContext dbContext) : IProcessedEventRepository
{
    public Task<bool> ExistsAsync(string eventId, CancellationToken cancellationToken) =>
        dbContext.ProcessedEvents.AnyAsync(processedEvent => processedEvent.EventId == eventId, cancellationToken);

    public void Add(ProcessedEvent processedEvent) => dbContext.ProcessedEvents.Add(processedEvent);
}

public sealed class CommissionSettingsRepository(CommissionsDbContext dbContext) : ICommissionSettingsRepository
{
    public Task<CommissionSchemaType> GetActiveSchemaAsync(CancellationToken cancellationToken) =>
        dbContext.Settings.AsNoTracking()
            .Where(settings => settings.Id == CommissionSettings.SingletonId)
            .Select(settings => settings.SchemaType)
            .SingleAsync(cancellationToken);

    public async Task<CommissionSettings> GetForUpdateAsync(CancellationToken cancellationToken)
    {
        var rows = await dbContext.Settings
            .FromSql($"SELECT * FROM commission_settings WHERE id = {CommissionSettings.SingletonId} FOR UPDATE")
            .ToListAsync(cancellationToken);
        return rows.Single();
    }

    public void AddChange(SchemaChange change) => dbContext.SchemaChanges.Add(change);
}

public sealed class PayoutReservationRepository(CommissionsDbContext dbContext) : IPayoutReservationRepository
{
    public async Task<PayoutReservation?> FindForUpdateAsync(Guid payoutId, CancellationToken cancellationToken)
    {
        // The row lock makes commands of the same payout run one after another.
        var rows = await dbContext.PayoutReservations
            .FromSql($"SELECT * FROM payout_reservations WHERE payout_id = {payoutId} FOR UPDATE")
            .ToListAsync(cancellationToken);
        return rows.SingleOrDefault();
    }

    public void Add(PayoutReservation reservation) => dbContext.PayoutReservations.Add(reservation);
}
