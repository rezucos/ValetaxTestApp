using ValetaxTest.Core;

namespace ValetaxTest.Commissions.Domain.Schemas;

public enum CommissionSchemaType
{
    Linear = 1,
    Fibonacci = 2,
}

/// <summary>
/// Commission schema: the percent of the profit a partner gets at a given level above the event owner.
/// The commission at level L is <c>rate(L) * Profit / 100</c>.
/// </summary>
public interface ICommissionSchema
{
    CommissionSchemaType Type { get; }

    /// <summary>Percent of the profit for a partner at <paramref name="level"/> (1 = direct partner).</summary>
    int GetRatePercent(int level);
}

/// <summary>Linear schema: <c>L * Profit / 100</c>.</summary>
public sealed class LinearCommissionSchema : ICommissionSchema
{
    public CommissionSchemaType Type => CommissionSchemaType.Linear;

    public int GetRatePercent(int level)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
        return level;
    }
}

/// <summary>
/// Fibonacci schema: <c>F(L) * Profit / 100</c>, where F is 1, 1, 2, 3, 5, 8, 13, 21, 34, 55 for levels 1-10
/// (each number is the sum of the two before it).
/// </summary>
public sealed class FibonacciCommissionSchema : ICommissionSchema
{
    /// <summary>F(46) is the largest Fibonacci number that fits into <see cref="int"/>.</summary>
    public const int MaxLevel = 46;

    public CommissionSchemaType Type => CommissionSchemaType.Fibonacci;

    public int GetRatePercent(int level)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(level, MaxLevel);

        var (previous, current) = (0, 1); // F(0), F(1)
        for (var i = 1; i < level; i++)
        {
            (previous, current) = (current, previous + current);
        }

        return current;
    }
}

/// <summary>Finds the schema by its type.</summary>
public sealed class CommissionSchemaResolver(IEnumerable<ICommissionSchema> schemas)
{
    private readonly Dictionary<CommissionSchemaType, ICommissionSchema> _schemas =
        schemas.ToDictionary(schema => schema.Type);

    public ICommissionSchema Resolve(CommissionSchemaType type) =>
        _schemas.TryGetValue(type, out var schema)
            ? schema
            : throw new DomainException($"Commission schema '{type}' is not supported.");
}
