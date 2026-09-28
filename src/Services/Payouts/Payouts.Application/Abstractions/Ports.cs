using ValetaxTest.Payouts.Application.Sagas;
using ValetaxTest.Payouts.Domain.Sagas;

namespace ValetaxTest.Payouts.Application.Abstractions;

public sealed record PartnersPage(IReadOnlyList<string> PartnerIds, bool HasMore);

/// <summary>Asks the Commissions service which partners have accrued commissions.</summary>
public interface ICommissionsLedger
{
    Task<PartnersPage> ListPartnersWithAccruedCommissionsAsync(string? afterPartnerId, int pageSize, CancellationToken cancellationToken);
}

/// <summary>Queries of payouts.</summary>
public interface IPayoutSagaReader
{
    /// <summary>Payouts matching the filters (<c>null</c> = any), newest first; pages start from 1.</summary>
    Task<IReadOnlyList<PayoutSummaryDto>> ListAsync(
        string? partnerId,
        PayoutSagaState? state,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PayoutDetailsDto?> GetAsync(Guid payoutId, CancellationToken cancellationToken);
}
