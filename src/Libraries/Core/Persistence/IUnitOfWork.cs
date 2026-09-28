namespace ValetaxTest.Core.Persistence;

/// <summary>Saves the changes made through repositories and the outbox in one transaction.</summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Starts a transaction for operations that read, check and write under a lock. Not needed for a single
    /// <see cref="SaveChangesAsync"/>; must not be used in message handlers, which already run in a transaction.
    /// </summary>
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}

public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}
