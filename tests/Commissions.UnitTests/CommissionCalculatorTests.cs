using ValetaxTest.Commissions.Domain.Accrual;
using ValetaxTest.Commissions.Domain.Schemas;

namespace ValetaxTest.Commissions.UnitTests;

public sealed class CommissionCalculatorTests
{
    private const int MaxLevels = 10;

    // Partners of the event owner: "c" invited the owner, "b" invited "c", and so on.
    private static readonly string[] Upline = ["c", "b", "a", "root"];

    private readonly CommissionCalculator _calculator = new();

    [Fact]
    public void Linear_PaysLevelPercentOfProfit()
    {
        var shares = _calculator.Calculate(1000m, Upline, new LinearCommissionSchema(), MaxLevels);

        Assert.Equal(Upline, shares.Select(share => share.PartnerId));
        Assert.Equal([1, 2, 3, 4], shares.Select(share => share.Level));
        Assert.Equal([1, 2, 3, 4], shares.Select(share => share.RatePercent));
        Assert.Equal([10m, 20m, 30m, 40m], shares.Select(share => share.Amount));
    }

    [Fact]
    public void Fibonacci_PaysFibonacciNumberPercentOfProfit()
    {
        var shares = _calculator.Calculate(1000m, Upline, new FibonacciCommissionSchema(), MaxLevels);

        Assert.Equal(Upline, shares.Select(share => share.PartnerId));
        Assert.Equal([1, 2, 3, 4], shares.Select(share => share.Level));
        Assert.Equal([1, 1, 2, 3], shares.Select(share => share.RatePercent));
        Assert.Equal([10m, 10m, 20m, 30m], shares.Select(share => share.Amount));
    }

    [Fact]
    public void LongChain_IsCutAtMaxLevels()
    {
        var upline = Enumerable.Range(1, 11).Select(level => $"partner-{level}").ToList();

        var shares = _calculator.Calculate(1000m, upline, new FibonacciCommissionSchema(), MaxLevels);

        Assert.Equal(upline.Take(10), shares.Select(share => share.PartnerId));
        Assert.Equal([10m, 10m, 20m, 30m, 50m, 80m, 130m, 210m, 340m, 550m], shares.Select(share => share.Amount));
    }

    // Over ten levels the linear schema pays 1 + 2 + ... + 10 = 55% of the profit,
    // the Fibonacci one 1 + 1 + 2 + ... + 55 = 143%.
    [Theory]
    [InlineData(CommissionSchemaType.Linear, 550)]
    [InlineData(CommissionSchemaType.Fibonacci, 1430)]
    public void TenLevels_PayTotalOfSchema(CommissionSchemaType schemaType, int expectedTotal)
    {
        var upline = Enumerable.Range(1, 10).Select(level => $"partner-{level}").ToList();
        ICommissionSchema schema = schemaType == CommissionSchemaType.Linear ? new LinearCommissionSchema() : new FibonacciCommissionSchema();

        var shares = _calculator.Calculate(1000m, upline, schema, MaxLevels);

        Assert.Equal(10, shares.Count);
        Assert.Equal(expectedTotal, shares.Sum(share => share.Amount));
    }

    [Fact]
    public void MaxLevelsBelowOne_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.Calculate(1000m, Upline, new LinearCommissionSchema(), 0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    public void NotPositiveProfit_PaysNothing(int profit)
    {
        Assert.Empty(_calculator.Calculate(profit, Upline, new LinearCommissionSchema(), MaxLevels));
    }

    [Fact]
    public void Amounts_AreRoundedTo8Digits()
    {
        var shares = _calculator.Calculate(33.33333333m, Upline, new LinearCommissionSchema(), MaxLevels);

        // Exact values: 0.3333333333, 0.6666666666, 0.9999999999, 1.3333333332.
        Assert.Equal([0.33333333m, 0.66666667m, 1m, 1.33333333m], shares.Select(share => share.Amount));
    }

    public static TheoryData<decimal, decimal> Midpoints => new()
    {
        { 0.0000015m, 0.00000002m }, // 0.000000015 goes up to the even digit
        { 0.0000025m, 0.00000002m }, // 0.000000025 goes down to the even digit
    };

    [Theory]
    [MemberData(nameof(Midpoints))]
    public void Midpoint_IsRoundedToEven(decimal profit, decimal expectedAmount)
    {
        var share = Assert.Single(_calculator.Calculate(profit, ["c"], new LinearCommissionSchema(), MaxLevels));

        Assert.Equal(expectedAmount, share.Amount);
    }

    [Fact]
    public void AmountRoundedToZero_IsSkipped()
    {
        // Level 1 gets 0.000000003, which is 0 with 8 digits; levels 2-4 get 0.00000001 each.
        var shares = _calculator.Calculate(0.0000003m, Upline, new LinearCommissionSchema(), MaxLevels);

        Assert.Equal([2, 3, 4], shares.Select(share => share.Level));
    }
}
