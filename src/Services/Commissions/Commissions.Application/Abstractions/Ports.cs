using ValetaxTest.Commissions.Application.Commissions;
using ValetaxTest.Commissions.Domain.Accrual;

namespace ValetaxTest.Commissions.Application.Abstractions;

/// <summary>Gets the partner chain of a user from the Partners service.</summary>
public interface IPartnerHierarchy
{
    /// <summary>
    /// Partners above the user by level (level 1 first), at most <paramref name="maxLevels"/>;
    /// <c>null</c> when the user is not registered.
    /// </summary>
    Task<IReadOnlyList<string>?> GetUplineAsync(string userId, int maxLevels, CancellationToken cancellationToken);
}

/// <summary>Queries of commissions.</summary>
public interface ICommissionReader
{
    /// <summary>Commissions matching the filters (<c>null</c> = any), newest first; pages start from 1.</summary>
    Task<IReadOnlyList<CommissionDto>> ListAsync(
        string? partnerId,
        string? eventId,
        CommissionStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PartnersPage> ListPartnersWithAccruedAsync(string? afterPartnerId, int pageSize, CancellationToken cancellationToken);

    Task<CommissionSchemaDto> GetSchemaAsync(int historySize, CancellationToken cancellationToken);
}
