namespace ValetaxTest.Partners.Domain.Users;

public interface IUserRepository
{
    Task<User?> FindAsync(string externalId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(string externalId, CancellationToken cancellationToken);

    /// <summary>All partners above the user (direct partner first), without a depth limit.</summary>
    Task<IReadOnlyList<string>> GetUplineIdsAsync(string externalId, CancellationToken cancellationToken);

    void Add(User user);
}
