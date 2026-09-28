using Microsoft.EntityFrameworkCore;
using Npgsql;
using ValetaxTest.Partners.Application.Abstractions;
using ValetaxTest.Partners.Application.Mappings;
using ValetaxTest.Partners.Application.Users;
using ValetaxTest.PersistencePostgres;

namespace ValetaxTest.Partners.Infrastructure.Persistence.Queries;

public sealed class PartnerTreeReader(PartnersDbContext dbContext) : IPartnerTreeReader
{
    public async Task<UserDto?> GetUserAsync(string externalId, CancellationToken cancellationToken)
    {
        var row = await dbContext.Users.AsNoTracking()
            .Where(user => user.ExternalId == externalId)
            .Select(user => new { User = user, ReferralsCount = dbContext.Users.Count(referral => referral.PartnerId == user.ExternalId) })
            .SingleOrDefaultAsync(cancellationToken);

        return row?.User.ToDto(row.ReferralsCount);
    }

    public async Task<IReadOnlyList<UserDto>> ListUsersAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var rows = await dbContext.Users.AsNoTracking()
            .OrderBy(user => user.CreatedAt).ThenBy(user => user.ExternalId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(user => new { User = user, ReferralsCount = dbContext.Users.Count(referral => referral.PartnerId == user.ExternalId) })
            .ToListAsync(cancellationToken);

        return rows.Select(row => row.User.ToDto(row.ReferralsCount)).ToList();
    }

    public async Task<IReadOnlyList<UplinePartnerDto>> GetUplineAsync(string externalId, int maxDepth, CancellationToken cancellationToken)
    {
        const string sql = """
            WITH RECURSIVE upline (user_id, level) AS (
                SELECT u.partner_id, 1
                FROM users u
                WHERE u.external_id = @userId AND u.partner_id IS NOT NULL
                UNION ALL
                SELECT p.partner_id, up.level + 1
                FROM upline up
                JOIN users p ON p.external_id = up.user_id
                WHERE p.partner_id IS NOT NULL AND up.level < @maxDepth
            )
            SELECT level, user_id FROM upline ORDER BY level
            """;

        return await dbContext.Database.QueryAsync(
            sql,
            reader => new UplinePartnerDto(reader.GetInt32(0), reader.GetString(1)),
            cancellationToken,
            new NpgsqlParameter("userId", externalId),
            new NpgsqlParameter("maxDepth", maxDepth));
    }

    public async Task<IReadOnlyList<DownlineRow>> GetDownlineAsync(string externalId, int maxDepth, int limit, CancellationToken cancellationToken)
    {
        const string sql = """
            WITH RECURSIVE downline (user_id, partner_id, level) AS (
                SELECT u.external_id, u.partner_id, 1
                FROM users u
                WHERE u.partner_id = @userId
                UNION ALL
                SELECT c.external_id, c.partner_id, d.level + 1
                FROM downline d
                JOIN users c ON c.partner_id = d.user_id
                WHERE d.level < @maxDepth
            )
            SELECT user_id, partner_id, level FROM downline ORDER BY level, user_id LIMIT @limit
            """;

        return await dbContext.Database.QueryAsync(
            sql,
            reader => new DownlineRow(reader.GetString(0), reader.GetString(1), reader.GetInt32(2)),
            cancellationToken,
            new NpgsqlParameter("userId", externalId),
            new NpgsqlParameter("maxDepth", maxDepth),
            new NpgsqlParameter("limit", limit));
    }
}
