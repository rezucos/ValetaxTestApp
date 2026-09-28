using ValetaxTest.Payouts.Domain.Sagas;

namespace ValetaxTest.Payouts.UnitTests;

public sealed class PayoutSagaStateMachineTests
{
    private static readonly TimeSpan StepTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMinutes(5);
    private static readonly DateTimeOffset Now = new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);

    // Commissions of partner "a" from scripts/demo.sh: 30 under the linear schema and 20 under the Fibonacci one.
    private static readonly PayoutSagaItem[] Items =
    [
        new(Guid.CreateVersion7(), "e1", 3, 30m, CommissionSchemaType.Linear),
        new(Guid.CreateVersion7(), "e3", 3, 20m, CommissionSchemaType.Fibonacci),
    ];

    private readonly PayoutSagaStateMachine _stateMachine = new(new PayoutSagaTiming(StepTimeout, MaxRetryDelay));

    [Fact]
    public void Start_WaitsForReservation()
    {
        var saga = _stateMachine.Start(Guid.CreateVersion7(), "a", Now);

        Assert.Equal(PayoutSagaState.Reserving, saga.State);
        Assert.Equal(PayoutSagaStep.ReserveCommissions, _stateMachine.PendingStep(saga));
        Assert.Equal(0m, saga.Amount);
        Assert.Equal(Now + StepTimeout, saga.NextAttemptAt);
        Assert.Null(saga.CompletedAt);
        var start = Assert.Single(saga.History);
        Assert.Null(start.From);
        Assert.Equal(PayoutSagaState.Reserving, start.To);
    }

    [Fact]
    public void SuccessfulPayout_ReservesCreditsAndConfirms()
    {
        var saga = _stateMachine.Start(Guid.CreateVersion7(), "a", Now);

        Assert.True(_stateMachine.OnCommissionsReserved(saga, Items, Now.AddSeconds(1)));
        Assert.Equal(PayoutSagaState.Crediting, saga.State);
        Assert.Equal(PayoutSagaStep.CreditWallet, _stateMachine.PendingStep(saga));
        Assert.Equal(Items, saga.Items);

        var creditedAt = Now.AddSeconds(2);
        Assert.True(_stateMachine.OnWalletCredited(saga, creditedAt, Now.AddSeconds(3)));
        Assert.Equal(PayoutSagaState.Confirming, saga.State);
        Assert.Equal(PayoutSagaStep.ConfirmPayment, _stateMachine.PendingStep(saga));
        Assert.Equal(creditedAt, saga.PaidAt);

        Assert.True(_stateMachine.OnPaymentConfirmed(saga, Now.AddSeconds(4)));
        Assert.Equal(PayoutSagaState.Completed, saga.State);
        Assert.Equal(PayoutSagaStep.None, _stateMachine.PendingStep(saga));
        Assert.Equal(Now.AddSeconds(4), saga.CompletedAt);
        Assert.Null(saga.NextAttemptAt);
        Assert.Null(saga.FailureReason);
        Assert.Equal(50m, saga.Amount);
        Assert.Equal(
            [PayoutSagaState.Reserving, PayoutSagaState.Crediting, PayoutSagaState.Confirming, PayoutSagaState.Completed],
            saga.History.Select(transition => transition.To));
    }

    [Fact]
    public void Amount_IsSumOfReservedCommissions()
    {
        var saga = _stateMachine.Start(Guid.CreateVersion7(), "a", Now);
        PayoutSagaItem[] items =
        [
            new(Guid.CreateVersion7(), "e1", 1, 0.1m, CommissionSchemaType.Linear),
            new(Guid.CreateVersion7(), "e2", 2, 0.2m, CommissionSchemaType.Linear),
            new(Guid.CreateVersion7(), "e3", 1, 0.00000001m, CommissionSchemaType.Fibonacci),
        ];

        _stateMachine.OnCommissionsReserved(saga, items, Now);

        Assert.Equal(0.30000001m, saga.Amount);
    }

    [Fact]
    public void NothingReserved_FinishesAtOnce()
    {
        var saga = _stateMachine.Start(Guid.CreateVersion7(), "a", Now);

        Assert.True(_stateMachine.OnCommissionsReserved(saga, [], Now.AddSeconds(1)));

        Assert.Equal(PayoutSagaState.NothingToPay, saga.State);
        Assert.Equal(PayoutSagaStep.None, _stateMachine.PendingStep(saga));
        Assert.Equal(0m, saga.Amount);
        Assert.Empty(saga.Items);
        Assert.Equal(Now.AddSeconds(1), saga.CompletedAt);
        Assert.Null(saga.NextAttemptAt);
    }

    [Fact]
    public void RejectedCredit_ReleasesCommissions()
    {
        var saga = _stateMachine.Start(Guid.CreateVersion7(), "a", Now);
        _stateMachine.OnCommissionsReserved(saga, Items, Now.AddSeconds(1));

        Assert.True(_stateMachine.OnWalletCreditRejected(saga, "Wallet is blocked", Now.AddSeconds(2)));
        Assert.Equal(PayoutSagaState.Compensating, saga.State);
        Assert.Equal(PayoutSagaStep.ReleaseCommissions, _stateMachine.PendingStep(saga));
        Assert.Equal("Wallet is blocked", saga.FailureReason);

        Assert.True(_stateMachine.OnCommissionsReleased(saga, Now.AddSeconds(3)));
        Assert.Equal(PayoutSagaState.Compensated, saga.State);
        Assert.Equal(PayoutSagaStep.None, _stateMachine.PendingStep(saga));
        Assert.Null(saga.PaidAt);
        Assert.Null(saga.NextAttemptAt);
        Assert.Equal(
            [PayoutSagaState.Reserving, PayoutSagaState.Crediting, PayoutSagaState.Compensating, PayoutSagaState.Compensated],
            saga.History.Select(transition => transition.To));
    }

    [Fact]
    public void RepeatedReservationReply_IsIgnored()
    {
        var saga = _stateMachine.Start(Guid.CreateVersion7(), "a", Now);
        _stateMachine.OnCommissionsReserved(saga, Items, Now.AddSeconds(1));
        var history = saga.History.Count;

        Assert.False(_stateMachine.OnCommissionsReserved(saga, [Items[0]], Now.AddSeconds(2)));

        Assert.Equal(PayoutSagaState.Crediting, saga.State);
        Assert.Equal(50m, saga.Amount);
        Assert.Equal(Items, saga.Items);
        Assert.Equal(history, saga.History.Count);
    }

    [Fact]
    public void ReplyBeforeItsStep_IsIgnored()
    {
        var saga = _stateMachine.Start(Guid.CreateVersion7(), "a", Now);

        Assert.False(_stateMachine.OnWalletCredited(saga, Now, Now));
        Assert.False(_stateMachine.OnWalletCreditRejected(saga, "Wallet is blocked", Now));
        Assert.False(_stateMachine.OnPaymentConfirmed(saga, Now));
        Assert.False(_stateMachine.OnCommissionsReleased(saga, Now));

        Assert.Equal(PayoutSagaState.Reserving, saga.State);
        Assert.Single(saga.History);
    }

    [Fact]
    public void RejectionAfterCredit_IsIgnored()
    {
        var saga = _stateMachine.Start(Guid.CreateVersion7(), "a", Now);
        _stateMachine.OnCommissionsReserved(saga, Items, Now);
        _stateMachine.OnWalletCredited(saga, Now, Now);

        Assert.False(_stateMachine.OnWalletCreditRejected(saga, "Wallet is blocked", Now));

        Assert.Equal(PayoutSagaState.Confirming, saga.State);
        Assert.Null(saga.FailureReason);
    }

    [Fact]
    public void RepliesAfterFinish_AreIgnored()
    {
        var saga = _stateMachine.Start(Guid.CreateVersion7(), "a", Now);
        _stateMachine.OnCommissionsReserved(saga, Items, Now);
        _stateMachine.OnWalletCredited(saga, Now, Now);
        _stateMachine.OnPaymentConfirmed(saga, Now);

        Assert.False(_stateMachine.OnCommissionsReserved(saga, Items, Now.AddSeconds(1)));
        Assert.False(_stateMachine.OnWalletCredited(saga, Now.AddSeconds(1), Now.AddSeconds(1)));
        Assert.False(_stateMachine.OnPaymentConfirmed(saga, Now.AddSeconds(1)));
        Assert.False(_stateMachine.OnCommissionsReleased(saga, Now.AddSeconds(1)));

        Assert.Equal(PayoutSagaState.Completed, saga.State);
        Assert.Equal(Now, saga.CompletedAt);
        Assert.Equal(50m, saga.Amount);
    }

    [Fact]
    public void Timeout_ResendsCommandWithDoublingDelay()
    {
        var saga = _stateMachine.Start(Guid.CreateVersion7(), "a", Now);

        // 30 s doubles on every retry and stops at MaxRetryDelay (5 min).
        TimeSpan[] delays =
        [
            TimeSpan.FromSeconds(60),
            TimeSpan.FromSeconds(120),
            TimeSpan.FromSeconds(240),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5),
        ];

        foreach (var delay in delays)
        {
            var at = saga.NextAttemptAt!.Value;
            Assert.Equal(PayoutSagaStep.ReserveCommissions, _stateMachine.OnStepTimeout(saga, at));
            Assert.Equal(at + delay, saga.NextAttemptAt);
        }

        Assert.Equal(delays.Length, saga.StepAttempts);
        Assert.Equal(PayoutSagaState.Reserving, saga.State);
        Assert.All(saga.History.Skip(1), transition => Assert.Equal(transition.From, transition.To));
    }

    [Fact]
    public void Timeout_NeverCancelsCredit()
    {
        var saga = _stateMachine.Start(Guid.CreateVersion7(), "a", Now);
        _stateMachine.OnCommissionsReserved(saga, Items, Now);

        for (var retry = 0; retry < 20; retry++)
        {
            Assert.Equal(PayoutSagaStep.CreditWallet, _stateMachine.OnStepTimeout(saga, saga.NextAttemptAt!.Value));
        }

        Assert.Equal(PayoutSagaState.Crediting, saga.State);
        Assert.Equal(20, saga.StepAttempts);
        Assert.Equal(50m, saga.Amount);
        Assert.Null(saga.FailureReason);
        Assert.Null(saga.CompletedAt);
    }

    [Fact]
    public void NextStep_ResetsRetries()
    {
        var saga = _stateMachine.Start(Guid.CreateVersion7(), "a", Now);
        _stateMachine.OnStepTimeout(saga, saga.NextAttemptAt!.Value);
        _stateMachine.OnStepTimeout(saga, saga.NextAttemptAt!.Value);
        var at = saga.NextAttemptAt!.Value;

        _stateMachine.OnCommissionsReserved(saga, Items, at);

        Assert.Equal(0, saga.StepAttempts);
        Assert.Equal(at + StepTimeout, saga.NextAttemptAt);
    }

    [Fact]
    public void Timeout_OfFinishedPayout_DoesNothing()
    {
        var saga = _stateMachine.Start(Guid.CreateVersion7(), "a", Now);
        _stateMachine.OnCommissionsReserved(saga, [], Now);
        var history = saga.History.Count;

        Assert.Equal(PayoutSagaStep.None, _stateMachine.OnStepTimeout(saga, Now.AddMinutes(1)));

        Assert.Equal(0, saga.StepAttempts);
        Assert.Null(saga.NextAttemptAt);
        Assert.Equal(history, saga.History.Count);
    }
}
