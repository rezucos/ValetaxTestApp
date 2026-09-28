using ValetaxTest.Partners.Application.Users;

namespace ValetaxTest.Partners.Application.Abstractions;

/// <summary>Queries of the partner tree.</summary>
public interface IPartnerTreeReader
{
    Task<UserDto?> GetUserAsync(string externalId, CancellationToken cancellationToken);

    /// <summary>Users in order of registration; pages start from 1.</summary>
    Task<IReadOnlyList<UserDto>> ListUsersAsync(int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Partners above the user ordered by level, at most <paramref name="maxDepth"/> levels.</summary>
    Task<IReadOnlyList<UplinePartnerDto>> GetUplineAsync(string externalId, int maxDepth, CancellationToken cancellationToken);

    /// <summary>
    /// Users below the user, level by level (nearest first): at most <paramref name="maxDepth"/> levels
    /// and <paramref name="limit"/> rows.
    /// </summary>
    Task<IReadOnlyList<DownlineRow>> GetDownlineAsync(string externalId, int maxDepth, int limit, CancellationToken cancellationToken);
}

public sealed record DownlineRow(string UserId, string PartnerId, int Level);
