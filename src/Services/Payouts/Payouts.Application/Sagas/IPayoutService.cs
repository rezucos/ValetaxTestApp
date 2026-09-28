using ValetaxTest.Payouts.Domain.Sagas;

namespace ValetaxTest.Payouts.Application.Sagas;

/// <summary>Payouts of accrued commissions to partners' wallets.</summary>
public interface IPayoutService
{
    /// <summary>
    /// Starts a payout for every partner with accrued commissions, except partners whose payout is still
    /// in progress. Called by the scheduler and through the API.
    /// </summary>
    Task<PayoutRunResult> RunAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Sends again the commands that got no reply in time (a service was down, a command went to a .dlt topic).
    /// This is safe: a repeated command gets the same result.
    /// </summary>
    /// <returns>Number of re-sent commands.</returns>
    Task<int> RetryTimedOutStepsAsync(CancellationToken cancellationToken);

    /// <summary>Payouts matching the filters (<c>null</c> = any), newest first; pages start from 1.</summary>
    Task<IReadOnlyList<PayoutSummaryDto>> ListAsync(
        string? partnerId,
        PayoutSagaState? state,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Payout with its commissions and the history of its states.</summary>
    Task<PayoutDetailsDto> GetAsync(Guid payoutId, CancellationToken cancellationToken);
}
