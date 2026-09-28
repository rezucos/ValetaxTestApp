using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using ValetaxTest.Core.Persistence;

namespace ValetaxTest.PersistencePostgres;

internal sealed class EfUnitOfWork<TContext>(TContext dbContext) : IUnitOfWork where TContext : DbContext
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException("The data was modified concurrently.", exception);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new DuplicateKeyException("A record with the same key already exists.", exception);
        }
    }

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken) =>
        new EfTransaction(await dbContext.Database.BeginTransactionAsync(cancellationToken));

    private sealed class EfTransaction(IDbContextTransaction transaction) : ITransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken) => transaction.CommitAsync(cancellationToken);

        // Disposing an uncommitted transaction rolls it back.
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}

/// <summary>PostgreSQL advisory locks that are released when the transaction ends.</summary>
internal sealed class PostgresTransactionalLock<TContext>(TContext dbContext) : ITransactionalLock where TContext : DbContext
{
    public async Task AcquireAsync(long key, CancellationToken cancellationToken)
    {
        if (dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Transaction-scoped advisory locks require an active transaction.");
        }

        await dbContext.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({key})", cancellationToken);
    }
}
