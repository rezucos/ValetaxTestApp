using ValetaxTest.Commissions.Domain.Schemas;

namespace ValetaxTest.Commissions.Domain.Accrual;

public sealed record AccrualResult(ProcessedEvent ProcessedEvent, IReadOnlyList<Commission> Commissions);

/// <summary>Turns a profit event into commissions and a record that the event is processed.</summary>
public sealed class CommissionAccrualService(CommissionCalculator calculator)
{
    /// <param name="upline">Partners above the event owner (level 1 first); <c>null</c> when the owner is not registered.</param>
    /// <param name="schema">Schema active now; it is stored in every commission.</param>
    public AccrualResult Accrue(
        AccrualRequest request,
        IReadOnlyList<string>? upline,
        ICommissionSchema schema,
        int maxLevels,
        DateTimeOffset now)
    {
        var shares = request.Profit > 0 && upline is not null
            ? calculator.Calculate(request.Profit, upline, schema, maxLevels)
            : [];

        var outcome = request.Profit <= 0 ? ProcessingOutcome.NotEligible
            : upline is null ? ProcessingOutcome.UnknownUser
            : upline.Count == 0 ? ProcessingOutcome.NoPartners
            : shares.Count == 0 ? ProcessingOutcome.NotEligible
            : ProcessingOutcome.Accrued;

        var commissions = shares
            .Select(share => new Commission
            {
                Id = Guid.CreateVersion7(),
                EventId = request.EventId,
                SourceUserId = request.UserId,
                PartnerId = share.PartnerId,
                Level = share.Level,
                RatePercent = share.RatePercent,
                Amount = share.Amount,
                SchemaType = schema.Type,
                Status = CommissionStatus.Accrued,
                AccruedAt = now,
            })
            .ToList();

        var processedEvent = new ProcessedEvent
        {
            EventId = request.EventId,
            UserId = request.UserId,
            Profit = request.Profit,
            OccurredAt = request.OccurredAt,
            Outcome = outcome,
            SchemaType = outcome == ProcessingOutcome.Accrued ? schema.Type : null,
            CommissionsCount = commissions.Count,
            CommissionsTotal = commissions.Sum(commission => commission.Amount),
            ProcessedAt = now,
        };

        return new AccrualResult(processedEvent, commissions);
    }
}
