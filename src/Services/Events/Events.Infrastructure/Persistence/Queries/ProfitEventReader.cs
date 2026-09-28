using Microsoft.EntityFrameworkCore;
using ValetaxTest.Events.Application.Abstractions;
using ValetaxTest.Events.Application.Mappings;
using ValetaxTest.Events.Application.ProfitEvents;

namespace ValetaxTest.Events.Infrastructure.Persistence.Queries;

public sealed class ProfitEventReader(EventsDbContext dbContext) : IProfitEventReader
{
    public async Task<IReadOnlyList<ProfitEventDto>> ListByUserAsync(string userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var profitEvents = await dbContext.ProfitEvents.AsNoTracking()
            .Where(profitEvent => profitEvent.UserId == userId)
            .OrderByDescending(profitEvent => profitEvent.OccurredAt).ThenBy(profitEvent => profitEvent.ExternalId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);

        return profitEvents.Select(profitEvent => profitEvent.ToDto()).ToList();
    }

    public async Task<ProfitEventDetailsDto?> GetDetailsAsync(string externalId, CancellationToken cancellationToken)
    {
        var profitEvent = await dbContext.ProfitEvents.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.ExternalId == externalId, cancellationToken);
        if (profitEvent is null)
        {
            return null;
        }

        var commissions = await (
                from commission in dbContext.EventCommissions.AsNoTracking()
                where commission.EventId == externalId
                join payment in dbContext.CommissionPayments.AsNoTracking()
                    on commission.CommissionId equals payment.CommissionId into payments
                from payment in payments.DefaultIfEmpty()
                orderby commission.Level
                select new { Commission = commission, Payment = payment })
            .ToListAsync(cancellationToken);

        return profitEvent.ToDetailsDto(commissions.Select(row => row.Commission.ToDto(row.Payment)).ToList());
    }
}
