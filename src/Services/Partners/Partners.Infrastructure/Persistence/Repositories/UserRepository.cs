using Microsoft.EntityFrameworkCore;
using Npgsql;
using ValetaxTest.Partners.Domain.Users;
using ValetaxTest.PersistencePostgres;

namespace ValetaxTest.Partners.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(PartnersDbContext dbContext) : IUserRepository
{
    public Task<User?> FindAsync(string externalId, CancellationToken cancellationToken) =>
        dbContext.Users.SingleOrDefaultAsync(user => user.ExternalId == externalId, cancellationToken);

    public Task<bool> ExistsAsync(string externalId, CancellationToken cancellationToken) =>
        dbContext.Users.AnyAsync(user => user.ExternalId == externalId, cancellationToken);

    public async Task<IReadOnlyList<string>> GetUplineIdsAsync(string externalId, CancellationToken cancellationToken)
    {
        // CYCLE is only a safeguard: the rules do not allow cycles, but a broken row must not make
        // this query loop forever.
        const string sql = """
            WITH RECURSIVE upline (user_id, level) AS (
                SELECT u.partner_id, 1
                FROM users u
                WHERE u.external_id = @userId AND u.partner_id IS NOT NULL
                UNION ALL
                SELECT p.partner_id, up.level + 1
                FROM upline up
                JOIN users p ON p.external_id = up.user_id
                WHERE p.partner_id IS NOT NULL
            ) CYCLE user_id SET is_cycle USING path
            SELECT user_id FROM upline WHERE NOT is_cycle ORDER BY level
            """;

        return await dbContext.Database.QueryAsync(
            sql, reader => reader.GetString(0), cancellationToken, new NpgsqlParameter("userId", externalId));
    }

    public void Add(User user) => dbContext.Users.Add(user);
}
