using Microsoft.EntityFrameworkCore;
using ValetaxTest.Commissions.Application.Abstractions;
using ValetaxTest.Commissions.Application.Commissions;
using ValetaxTest.Commissions.Application.Mappings;
using ValetaxTest.Commissions.Domain.Accrual;
using ValetaxTest.Commissions.Domain.Schemas;

namespace ValetaxTest.Commissions.Infrastructure.Persistence.Queries;

public sealed class CommissionReader(CommissionsDbContext dbContext) : ICommissionReader
{
    public async Task<IReadOnlyList<CommissionDto>> ListAsync(
        string? partnerId,
        string? eventId,
        CommissionStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var commissions = dbContext.Commissions.AsNoTracking();
        if (partnerId is not null)
        {
            commissions = commissions.Where(commission => commission.PartnerId == partnerId);
        }

        if (eventId is not null)
        {
            commissions = commissions.Where(commission => commission.EventId == eventId);
        }

        if (status is not null)
        {
            commissions = commissions.Where(commission => commission.Status == status);
        }

        var rows = await commissions
            .OrderByDescending(commission => commission.AccruedAt).ThenBy(commission => commission.Level)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);

        return rows.Select(commission => commission.ToDto()).ToList();
    }

    public async Task<PartnersPage> ListPartnersWithAccruedAsync(string? afterPartnerId, int pageSize, CancellationToken cancellationToken)
    {
        var accrued = dbContext.Commissions.AsNoTracking().Where(commission => commission.Status == CommissionStatus.Accrued);
        if (afterPartnerId is not null)
        {
            accrued = accrued.Where(commission => string.Compare(commission.PartnerId, afterPartnerId) > 0);
        }

        var partnerIds = await accrued
            .Select(commission => commission.PartnerId)
            .Distinct()
            .OrderBy(partnerId => partnerId)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = partnerIds.Count > pageSize;
        return new PartnersPage(hasMore ? partnerIds.Take(pageSize).ToList() : partnerIds, hasMore);
    }

    public async Task<CommissionSchemaDto> GetSchemaAsync(int historySize, CancellationToken cancellationToken)
    {
        var settings = await dbContext.Settings.AsNoTracking()
            .SingleAsync(candidate => candidate.Id == CommissionSettings.SingletonId, cancellationToken);

        var history = await dbContext.SchemaChanges.AsNoTracking()
            .OrderByDescending(change => change.Id)
            .Take(historySize)
            .ToListAsync(cancellationToken);

        return settings.ToDto(history.Select(change => change.ToDto()).ToList());
    }
}
