namespace ValetaxTest.Core.Persistence;

/// <summary>A lock shared by all replicas of the service; it is held until the current transaction ends.</summary>
public interface ITransactionalLock
{
    /// <summary>Waits until the lock is available.</summary>
    Task AcquireAsync(long key, CancellationToken cancellationToken);
}
