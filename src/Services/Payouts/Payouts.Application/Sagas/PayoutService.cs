using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ValetaxTest.Core.Application;
using ValetaxTest.Core.Persistence;
using ValetaxTest.Payouts.Application.Abstractions;
using ValetaxTest.Payouts.Application.Mappings;
using ValetaxTest.Payouts.Domain.Sagas;

namespace ValetaxTest.Payouts.Application.Sagas;

public sealed class PayoutService(
    ICommissionsLedger ledger,
    IPayoutSagaRepository sagas,
    IPayoutSagaReader reader,
    PayoutSagaStateMachine stateMachine,
    PayoutCommandSender commandSender,
    ITransactionalLock transactionalLock,
    IUnitOfWork unitOfWork,
    IOptions<PayoutOptions> options,
    PayoutsMetrics metrics,
    TimeProvider timeProvider,
    ILogger<PayoutService> logger) : IPayoutService
{
    private const int LedgerPageSize = 500;
    private const int TimeoutBatchSize = 100;

    public async Task<PayoutRunResult> RunAsync(CancellationToken cancellationToken)
    {
        // Partners are read from Commissions before the transaction starts, so the call does not keep the transaction
        // and the lock open. They are read page by page, at most MaxPartnersPerRun; the rest wait for the next run.
        var maxPartners = options.Value.MaxPartnersPerRun;
        var partners = new List<string>();
        string? after = null;
        while (partners.Count < maxPartners)
        {
            var page = await ledger.ListPartnersWithAccruedCommissionsAsync(
                after, Math.Min(LedgerPageSize, maxPartners - partners.Count), cancellationToken);
            partners.AddRange(page.PartnerIds);

            if (!page.HasMore || page.PartnerIds.Count == 0)
            {
                break;
            }

            after = page.PartnerIds[^1];
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        // Runs of all replicas start payouts one at a time: a run that waited here sees the payouts started by
        // the previous one as in progress. A partner paid in between gets a payout that ends with NothingToPay.
        await transactionalLock.AcquireAsync(LockKeys.PayoutRun, cancellationToken);

        var busy = await sagas.GetPartnersWithActiveSagasAsync(partners, cancellationToken);
        var now = timeProvider.GetUtcNow();

        var started = new List<StartedPayout>();
        foreach (var partnerId in partners.Where(partnerId => !busy.Contains(partnerId)))
        {
            var saga = stateMachine.Start(Guid.CreateVersion7(), partnerId, now);
            sagas.Add(saga);
            commandSender.SendPendingCommand(saga);
            started.Add(saga.ToStartedPayout());
        }

        if (started.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        metrics.SagasStarted(started.Count);
        logger.LogInformation(
            "Payout run: {Partners} partner(s) with accrued commissions, {Started} payout(s) started, {Busy} already in progress",
            partners.Count, started.Count, busy.Count);

        return new PayoutRunResult(partners.Count, started);
    }

    public async Task<int> RetryTimedOutStepsAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var now = timeProvider.GetUtcNow();
        var timedOut = await sagas.LockTimedOutAsync(now, TimeoutBatchSize, cancellationToken);
        foreach (var saga in timedOut)
        {
            var step = stateMachine.OnStepTimeout(saga, now);
            commandSender.SendPendingCommand(saga);
            metrics.StepRetried(step);
            logger.LogWarning("Payout {PayoutId} got no reply to {Step} in time, command re-sent (retry {Retry})",
                saga.Id, step, saga.StepAttempts);
        }

        if (timedOut.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return timedOut.Count;
    }

    public Task<IReadOnlyList<PayoutSummaryDto>> ListAsync(
        string? partnerId,
        PayoutSagaState? state,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        Pages.Validate(page, pageSize);
        return reader.ListAsync(partnerId, state, page, pageSize, cancellationToken);
    }

    public async Task<PayoutDetailsDto> GetAsync(Guid payoutId, CancellationToken cancellationToken) =>
        await reader.GetAsync(payoutId, cancellationToken)
        ?? throw new NotFoundException($"Payout {payoutId} does not exist.");
}
