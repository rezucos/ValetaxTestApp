namespace ValetaxTest.Commissions.Domain.Schemas;

/// <summary>Settings of the commission program (always one row): the schema used for new calculations.</summary>
public sealed class CommissionSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; }

    public CommissionSchemaType SchemaType { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>A record of a schema switch.</summary>
public sealed class SchemaChange
{
    public long Id { get; set; }

    public CommissionSchemaType PreviousSchemaType { get; set; }

    public CommissionSchemaType SchemaType { get; set; }

    public DateTimeOffset ChangedAt { get; set; }
}

public interface ICommissionSettingsRepository
{
    /// <summary>Schema used for new calculations.</summary>
    Task<CommissionSchemaType> GetActiveSchemaAsync(CancellationToken cancellationToken);

    /// <summary>Loads the settings to change them; the row stays locked until the transaction ends.</summary>
    Task<CommissionSettings> GetForUpdateAsync(CancellationToken cancellationToken);

    void AddChange(SchemaChange change);
}

/// <summary>Switches between commission schemas.</summary>
public sealed class CommissionSchemaService
{
    /// <summary>
    /// Makes <paramref name="schemaType"/> the schema of new calculations. Commissions accrued before are not
    /// recalculated: each keeps the schema it was calculated with.
    /// </summary>
    /// <returns>The record of the switch, or <c>null</c> when the schema is already active.</returns>
    public SchemaChange? Switch(CommissionSettings settings, CommissionSchemaType schemaType, DateTimeOffset now)
    {
        if (settings.SchemaType == schemaType)
        {
            return null;
        }

        var change = new SchemaChange
        {
            PreviousSchemaType = settings.SchemaType,
            SchemaType = schemaType,
            ChangedAt = now,
        };

        settings.SchemaType = schemaType;
        settings.UpdatedAt = now;
        return change;
    }
}
