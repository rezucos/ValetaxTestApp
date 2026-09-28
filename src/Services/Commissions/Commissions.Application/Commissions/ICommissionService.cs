using ValetaxTest.Commissions.Domain.Accrual;

namespace ValetaxTest.Commissions.Application.Commissions;

/// <summary>Accrued commissions and the schema used for new calculations.</summary>
public interface ICommissionService
{
    /// <summary>Commissions matching the filters (<c>null</c> = any), newest first; pages start from 1.</summary>
    Task<IReadOnlyList<CommissionDto>> ListAsync(
        string? partnerId,
        string? eventId,
        CommissionStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Partners that have accrued commissions, sorted by id, starting after <paramref name="afterPartnerId"/>.
    /// The Payouts service uses it to find whom to pay.
    /// </summary>
    Task<PartnersPage> ListPartnersWithAccruedAsync(string? afterPartnerId, int pageSize, CancellationToken cancellationToken);

    Task<CommissionSchemaDto> GetSchemaAsync(CancellationToken cancellationToken);

    /// <summary>Switches between Linear and Fibonacci. Only new calculations use the new schema.</summary>
    Task<CommissionSchemaDto> SwitchSchemaAsync(SwitchSchemaRequest request, CancellationToken cancellationToken);
}
