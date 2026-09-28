using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace ValetaxTest.PersistencePostgres;

public static class DatabaseFacadeExtensions
{
    /// <summary>Runs a raw SQL query that LINQ cannot express (such as a recursive one) and maps each row.</summary>
    public static async Task<List<T>> QueryAsync<T>(
        this DatabaseFacade database,
        string sql,
        Func<DbDataReader, T> map,
        CancellationToken cancellationToken,
        params NpgsqlParameter[] parameters)
    {
        // EF Core closes the connection when the DbContext is disposed.
        await database.OpenConnectionAsync(cancellationToken);

        await using var command = database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        command.Transaction = database.CurrentTransaction?.GetDbTransaction();
        command.Parameters.AddRange(parameters);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<T>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(map(reader));
        }

        return rows;
    }
}
