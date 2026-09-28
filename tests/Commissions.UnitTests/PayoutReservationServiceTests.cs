using ValetaxTest.Commissions.Domain.Accrual;
using ValetaxTest.Commissions.Domain.Payouts;
using ValetaxTest.Commissions.Domain.Schemas;
using ValetaxTest.Core;

namespace ValetaxTest.Commissions.UnitTests;

public sealed class PayoutReservationServiceTests
{
    private static readonly DateTimeOffset AccruedAt = new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = AccruedAt.AddHours(1);

    // Partners of the event owner "d": "c" invited "d", "b" invited "c", and so on.
    private static readonly string[] Upline = ["c", "b", "a", "root"];

    private readonly PayoutReservationService _service = new();

    [Fact]
    public void Reserve_TotalIsSumOfAccruedCommissions()
    {
        // "a" is at level 3: 3% of 1000 under the linear schema, F(3) = 2% under the Fibonacci one.
        var commissions = CommissionsOf("a", Accrue("e1", 1000m, new LinearCommissionSchema()), Accrue("e3", 1000m, new FibonacciCommissionSchema()));
        var payoutId = Guid.CreateVersion7();

        var reservation = _service.Reserve(payoutId, "a", commissions, Now);

        Assert.Equal(ReservationStatus.Reserved, reservation.Status);
        Assert.Equal(50m, reservation.TotalAmount);
        Assert.Equal(["e1", "e3"], reservation.Items.Select(item => item.EventId));
        Assert.Equal([30m, 20m], reservation.Items.Select(item => item.Amount));
        Assert.Equal([CommissionSchemaType.Linear, CommissionSchemaType.Fibonacci], reservation.Items.Select(item => item.SchemaType));
        Assert.All(commissions, commission =>
        {
            Assert.Equal(CommissionStatus.Reserved, commission.Status);
            Assert.Equal(payoutId, commission.PayoutId);
            Assert.Equal(Now, commission.ReservedAt);
        });
    }

    [Fact]
    public void Reserve_WithoutAccruedCommissions_ReservesNothing()
    {
        var reservation = _service.Reserve(Guid.CreateVersion7(), "a", [], Now);

        Assert.Equal(ReservationStatus.Reserved, reservation.Status);
        Assert.Equal(0m, reservation.TotalAmount);
        Assert.Empty(reservation.Items);
    }

    [Theory]
    [InlineData(CommissionStatus.Reserved)]
    [InlineData(CommissionStatus.Paid)]
    public void Reserve_RejectsCommissionThatIsNotAccrued(CommissionStatus status)
    {
        var commissions = CommissionsOf("a", Accrue("e1", 1000m, new LinearCommissionSchema()));
        commissions[0].Status = status;

        Assert.Throws<DomainException>(() => _service.Reserve(Guid.CreateVersion7(), "a", commissions, Now));
    }

    [Fact]
    public void Reserve_RejectsCommissionOfAnotherPartner()
    {
        var commissions = CommissionsOf("b", Accrue("e1", 1000m, new LinearCommissionSchema()));

        Assert.Throws<DomainException>(() => _service.Reserve(Guid.CreateVersion7(), "a", commissions, Now));
    }

    [Fact]
    public void ConfirmPaid_MarksReservedCommissionsAsPaid()
    {
        var commissions = CommissionsOf("a", Accrue("e1", 1000m, new LinearCommissionSchema()), Accrue("e3", 1000m, new FibonacciCommissionSchema()));
        var reservation = _service.Reserve(Guid.CreateVersion7(), "a", commissions, Now);
        var paidAt = Now.AddMinutes(1);

        Assert.True(_service.ConfirmPaid(reservation, commissions, paidAt));

        Assert.Equal(ReservationStatus.Paid, reservation.Status);
        Assert.Equal(paidAt, reservation.CompletedAt);
        Assert.Equal(50m, reservation.TotalAmount);
        Assert.All(commissions, commission =>
        {
            Assert.Equal(CommissionStatus.Paid, commission.Status);
            Assert.Equal(reservation.PayoutId, commission.PayoutId);
            Assert.Equal(paidAt, commission.PaidAt);
        });
        Assert.Equal([30m, 20m], commissions.Select(commission => commission.Amount));
    }

    [Fact]
    public void ConfirmPaid_Repeated_ChangesNothing()
    {
        var commissions = CommissionsOf("a", Accrue("e1", 1000m, new LinearCommissionSchema()));
        var reservation = _service.Reserve(Guid.CreateVersion7(), "a", commissions, Now);
        var paidAt = Now.AddMinutes(1);
        _service.ConfirmPaid(reservation, commissions, paidAt);

        Assert.False(_service.ConfirmPaid(reservation, commissions, paidAt.AddMinutes(1)));

        Assert.Equal(paidAt, reservation.CompletedAt);
        Assert.Equal(paidAt, commissions[0].PaidAt);
    }

    [Fact]
    public void ConfirmPaid_AfterRelease_Throws()
    {
        var commissions = CommissionsOf("a", Accrue("e1", 1000m, new LinearCommissionSchema()));
        var reservation = _service.Reserve(Guid.CreateVersion7(), "a", commissions, Now);
        _service.Release(reservation, commissions, "Wallet is blocked", Now);

        Assert.Throws<DomainException>(() => _service.ConfirmPaid(reservation, commissions, Now));
    }

    [Fact]
    public void ConfirmPaid_WithMissingCommission_Throws()
    {
        var commissions = CommissionsOf("a", Accrue("e1", 1000m, new LinearCommissionSchema()), Accrue("e3", 1000m, new FibonacciCommissionSchema()));
        var reservation = _service.Reserve(Guid.CreateVersion7(), "a", commissions, Now);

        Assert.Throws<DomainException>(() => _service.ConfirmPaid(reservation, [commissions[0]], Now));
        Assert.Equal(ReservationStatus.Reserved, reservation.Status);
    }

    [Fact]
    public void Release_ReturnsCommissionsToAccrued()
    {
        var commissions = CommissionsOf("a", Accrue("e1", 1000m, new LinearCommissionSchema()));
        var reservation = _service.Reserve(Guid.CreateVersion7(), "a", commissions, Now);

        Assert.True(_service.Release(reservation, commissions, "Wallet is blocked", Now));

        Assert.Equal(ReservationStatus.Released, reservation.Status);
        Assert.Equal("Wallet is blocked", reservation.ReleaseReason);
        Assert.Equal(Now, reservation.CompletedAt);
        Assert.Equal(CommissionStatus.Accrued, commissions[0].Status);
        Assert.Null(commissions[0].PayoutId);
        Assert.Null(commissions[0].ReservedAt);
        Assert.Null(commissions[0].PaidAt);
    }

    [Fact]
    public void ReleasedCommissions_ArePaidByNextPayout()
    {
        var commissions = CommissionsOf("a", Accrue("e1", 1000m, new LinearCommissionSchema()), Accrue("e3", 1000m, new FibonacciCommissionSchema()));
        var rejected = _service.Reserve(Guid.CreateVersion7(), "a", commissions, Now);
        _service.Release(rejected, commissions, "Wallet is blocked", Now);

        var next = _service.Reserve(Guid.CreateVersion7(), "a", commissions, Now.AddMinutes(1));
        _service.ConfirmPaid(next, commissions, Now.AddMinutes(2));

        Assert.Equal(50m, next.TotalAmount);
        Assert.All(commissions, commission =>
        {
            Assert.Equal(CommissionStatus.Paid, commission.Status);
            Assert.Equal(next.PayoutId, commission.PayoutId);
        });
    }

    [Fact]
    public void Release_Repeated_ChangesNothing()
    {
        var commissions = CommissionsOf("a", Accrue("e1", 1000m, new LinearCommissionSchema()));
        var reservation = _service.Reserve(Guid.CreateVersion7(), "a", commissions, Now);
        _service.Release(reservation, commissions, "Wallet is blocked", Now);

        Assert.False(_service.Release(reservation, commissions, "Another reason", Now.AddMinutes(1)));

        Assert.Equal("Wallet is blocked", reservation.ReleaseReason);
        Assert.Equal(Now, reservation.CompletedAt);
    }

    [Fact]
    public void Release_AfterPayment_Throws()
    {
        var commissions = CommissionsOf("a", Accrue("e1", 1000m, new LinearCommissionSchema()));
        var reservation = _service.Reserve(Guid.CreateVersion7(), "a", commissions, Now);
        _service.ConfirmPaid(reservation, commissions, Now);

        Assert.Throws<DomainException>(() => _service.Release(reservation, commissions, "Wallet is blocked", Now));
        Assert.Equal(CommissionStatus.Paid, commissions[0].Status);
    }

    [Fact]
    public void RegisterReleased_ReservesNothing()
    {
        var reservation = _service.RegisterReleased(Guid.CreateVersion7(), "a", "Wallet is blocked", Now);

        Assert.Equal(ReservationStatus.Released, reservation.Status);
        Assert.Equal(0m, reservation.TotalAmount);
        Assert.Empty(reservation.Items);
        Assert.Equal("Wallet is blocked", reservation.ReleaseReason);
    }

    // The same events as in scripts/demo.sh: 1000 under the linear schema, then 1000 under the Fibonacci one.
    [Theory]
    [InlineData("c", 20)]
    [InlineData("b", 30)]
    [InlineData("a", 50)]
    [InlineData("root", 70)]
    public void Payout_PaysEverythingAccruedToPartner(string partnerId, int expectedAmount)
    {
        var commissions = CommissionsOf(partnerId, Accrue("e1", 1000m, new LinearCommissionSchema()), Accrue("e3", 1000m, new FibonacciCommissionSchema()));
        var reservation = _service.Reserve(Guid.CreateVersion7(), partnerId, commissions, Now);
        _service.ConfirmPaid(reservation, commissions, Now);

        Assert.Equal(expectedAmount, reservation.TotalAmount);
        Assert.Equal(expectedAmount, commissions.Where(commission => commission.Status == CommissionStatus.Paid).Sum(commission => commission.Amount));
    }

    private static IReadOnlyList<Commission> Accrue(string eventId, decimal profit, ICommissionSchema schema) =>
        new CommissionAccrualService(new CommissionCalculator())
            .Accrue(new AccrualRequest(eventId, "d", profit, AccruedAt), Upline, schema, 10, AccruedAt)
            .Commissions;

    private static List<Commission> CommissionsOf(string partnerId, params IReadOnlyList<Commission>[] accruals) =>
        accruals.SelectMany(commissions => commissions).Where(commission => commission.PartnerId == partnerId).ToList();
}
