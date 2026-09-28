using ValetaxTest.Commissions.Domain.Schemas;
using ValetaxTest.Core;

namespace ValetaxTest.Commissions.UnitTests;

public sealed class CommissionSchemasTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(10, 10)]
    public void Linear_RateIsLevel(int level, int expectedRate)
    {
        Assert.Equal(expectedRate, new LinearCommissionSchema().GetRatePercent(level));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    [InlineData(4, 3)]
    [InlineData(5, 5)]
    [InlineData(6, 8)]
    [InlineData(7, 13)]
    [InlineData(8, 21)]
    [InlineData(9, 34)]
    [InlineData(10, 55)]
    public void Fibonacci_RateIsFibonacciNumberOfLevel(int level, int expectedRate)
    {
        Assert.Equal(expectedRate, new FibonacciCommissionSchema().GetRatePercent(level));
    }

    [Fact]
    public void LevelBelowOne_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LinearCommissionSchema().GetRatePercent(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FibonacciCommissionSchema().GetRatePercent(0));
    }

    [Fact]
    public void Fibonacci_LevelAboveMax_IsRejected()
    {
        Assert.Equal(1_836_311_903, new FibonacciCommissionSchema().GetRatePercent(FibonacciCommissionSchema.MaxLevel));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FibonacciCommissionSchema().GetRatePercent(FibonacciCommissionSchema.MaxLevel + 1));
    }

    [Theory]
    [InlineData(CommissionSchemaType.Linear)]
    [InlineData(CommissionSchemaType.Fibonacci)]
    public void Resolver_FindsSchemaByType(CommissionSchemaType schemaType)
    {
        var resolver = new CommissionSchemaResolver([new LinearCommissionSchema(), new FibonacciCommissionSchema()]);

        Assert.Equal(schemaType, resolver.Resolve(schemaType).Type);
    }

    [Fact]
    public void Resolver_UnknownType_Throws()
    {
        var resolver = new CommissionSchemaResolver([new LinearCommissionSchema(), new FibonacciCommissionSchema()]);

        Assert.Throws<DomainException>(() => resolver.Resolve((CommissionSchemaType)99));
    }
}
