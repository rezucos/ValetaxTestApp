using ValetaxTest.Commissions.Domain.Accrual;
using ValetaxTest.Commissions.Domain.Schemas;

namespace ValetaxTest.Commissions.UnitTests;

public sealed class CommissionSchemaServiceTests
{
    private static readonly DateTimeOffset Created = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);

    private readonly CommissionSchemaService _service = new();

    [Fact]
    public void Switch_ActivatesSchemaAndRecordsChange()
    {
        var settings = LinearSettings();

        var change = _service.Switch(settings, CommissionSchemaType.Fibonacci, Now);

        Assert.NotNull(change);
        Assert.Equal(CommissionSchemaType.Linear, change.PreviousSchemaType);
        Assert.Equal(CommissionSchemaType.Fibonacci, change.SchemaType);
        Assert.Equal(CommissionSchemaType.Fibonacci, settings.SchemaType);
        Assert.Equal(Now, settings.UpdatedAt);
    }

    [Fact]
    public void SwitchToActiveSchema_ChangesNothing()
    {
        var settings = LinearSettings();

        Assert.Null(_service.Switch(settings, CommissionSchemaType.Linear, Now));
        Assert.Equal(Created, settings.UpdatedAt);
    }

    [Fact]
    public void Switch_AffectsOnlyNewCalculations()
    {
        var settings = LinearSettings();
        var schemas = new CommissionSchemaResolver([new LinearCommissionSchema(), new FibonacciCommissionSchema()]);
        var accrual = new CommissionAccrualService(new CommissionCalculator());
        string[] upline = ["c", "b", "a", "root"];

        var before = accrual.Accrue(new AccrualRequest("e1", "d", 1000m, Now), upline, schemas.Resolve(settings.SchemaType), 10, Now);
        _service.Switch(settings, CommissionSchemaType.Fibonacci, Now);
        var after = accrual.Accrue(new AccrualRequest("e2", "d", 1000m, Now), upline, schemas.Resolve(settings.SchemaType), 10, Now);

        Assert.All(before.Commissions, commission => Assert.Equal(CommissionSchemaType.Linear, commission.SchemaType));
        Assert.Equal([10m, 20m, 30m, 40m], before.Commissions.Select(commission => commission.Amount));
        Assert.All(after.Commissions, commission => Assert.Equal(CommissionSchemaType.Fibonacci, commission.SchemaType));
        Assert.Equal([10m, 10m, 20m, 30m], after.Commissions.Select(commission => commission.Amount));
    }

    private static CommissionSettings LinearSettings() => new()
    {
        Id = CommissionSettings.SingletonId,
        SchemaType = CommissionSchemaType.Linear,
        UpdatedAt = Created,
    };
}
