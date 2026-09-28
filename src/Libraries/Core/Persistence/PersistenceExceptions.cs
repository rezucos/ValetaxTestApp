namespace ValetaxTest.Core.Persistence;

/// <summary>A write conflicted with a concurrent write of the same data.</summary>
public abstract class PersistenceConflictException(string message, Exception innerException)
    : Exception(message, innerException);

/// <summary>A record with the same unique key already exists (usually the same request sent twice at once).</summary>
public sealed class DuplicateKeyException(string message, Exception innerException)
    : PersistenceConflictException(message, innerException);

/// <summary>The record was changed by someone else after it had been read.</summary>
public sealed class ConcurrencyConflictException(string message, Exception innerException)
    : PersistenceConflictException(message, innerException);
