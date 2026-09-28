using ValetaxTest.Commissions.Domain.Accrual;
using ValetaxTest.Commissions.Domain.Schemas;

namespace ValetaxTest.Commissions.UnitTests;

public sealed class CommissionAccrualServiceTests
{
    private const int MaxLevels = 10;

    private static readonly DateTimeOffset Now = new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);

    private static readonly CommissionSchemaResolver Schemas =
        new([new LinearCommissionSchema(), new FibonacciCommissionSchema()]);

    private readonly CommissionAccrualService _service = new(new CommissionCalculator());

    [Theory]
    [InlineData(CommissionSchemaType.Linear, 60)]
    [InlineData(CommissionSchemaType.Fibonacci, 40)]
    public void Commissions_StoreSchemaType(CommissionSchemaType schemaType, int expectedTotal)
    {
        var result = _service.Accrue(Request(1000m), ["c", "b", "a"], Schemas.Resolve(schemaType), MaxLevels, Now);

        Assert.Equal(3, result.Commissions.Count);
        Assert.All(result.Commissions, commission =>
        {
            Assert.Equal(schemaType, commission.SchemaType);
            Assert.Equal("e1", commission.EventId);
            Assert.Equal("d", commission.SourceUserId);
            Assert.Equal(CommissionStatus.Accrued, commission.Status);
        });

        Assert.Equal(ProcessingOutcome.Accrued, result.ProcessedEvent.Outcome);
        Assert.Equal(schemaType, result.ProcessedEvent.SchemaType);
        Assert.Equal(3, result.ProcessedEvent.CommissionsCount);
        Assert.Equal(expectedTotal, result.ProcessedEvent.CommissionsTotal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    public void NotPositiveProfit_IsNotEligible(int profit)
    {
        var result = _service.Accrue(Request(profit), ["c"], new LinearCommissionSchema(), MaxLevels, Now);

        Assert.Empty(result.Commissions);
        Assert.Equal(ProcessingOutcome.NotEligible, result.ProcessedEvent.Outcome);
        Assert.Null(result.ProcessedEvent.SchemaType);
    }

    [Fact]
    public void TooSmallProfit_IsNotEligible()
    {
        var result = _service.Accrue(Request(0.00000001m), ["c"], new LinearCommissionSchema(), MaxLevels, Now);

        Assert.Empty(result.Commissions);
        Assert.Equal(ProcessingOutcome.NotEligible, result.ProcessedEvent.Outcome);
    }

    [Fact]
    public void UnknownOwner_IsUnknownUser()
    {
        var result = _service.Accrue(Request(1000m), upline: null, new LinearCommissionSchema(), MaxLevels, Now);

        Assert.Empty(result.Commissions);
        Assert.Equal(ProcessingOutcome.UnknownUser, result.ProcessedEvent.Outcome);
    }

    [Fact]
    public void OwnerWithoutPartners_HasNoPartners()
    {
        var result = _service.Accrue(Request(1000m), [], new LinearCommissionSchema(), MaxLevels, Now);

        Assert.Empty(result.Commissions);
        Assert.Equal(ProcessingOutcome.NoPartners, result.ProcessedEvent.Outcome);
    }

    private static AccrualRequest Request(decimal profit) => new("e1", "d", profit, Now);
}
