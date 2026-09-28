namespace ValetaxTest.Payouts.Domain.Sagas;

/// <param name="StepTimeout">How long to wait for a reply before re-sending the command of the step.</param>
/// <param name="MaxRetryDelay">Longest pause between re-sends (the pause doubles each time).</param>
public sealed record PayoutSagaTiming(TimeSpan StepTimeout, TimeSpan MaxRetryDelay);

/// <summary>
/// States of a payout and the replies that move it on. Normally the commissions are reserved, the wallet
/// is credited and the commissions are marked as paid. If the wallet rejects the payout, the reserved
/// commissions are released. If there is nothing to reserve, the payout ends at once.
/// </summary>
public sealed class PayoutSagaStateMachine(PayoutSagaTiming timing)
{
    public static readonly IReadOnlyList<PayoutSagaState> ActiveStates =
    [
        PayoutSagaState.Reserving,
        PayoutSagaState.Crediting,
        PayoutSagaState.Confirming,
        PayoutSagaState.Compensating,
    ];

    public PayoutSaga Start(Guid payoutId, string partnerId, DateTimeOffset now)
    {
        var saga = new PayoutSaga
        {
            Id = payoutId,
            PartnerId = partnerId,
            State = PayoutSagaState.Reserving,
            CreatedAt = now,
            UpdatedAt = now,
            NextAttemptAt = now + timing.StepTimeout,
        };

        saga.History.Add(new PayoutSagaTransition(now, null, PayoutSagaState.Reserving, "Payout started: reserving accrued commissions"));
        return saga;
    }

    /// <summary>The command whose reply the saga is waiting for.</summary>
    public PayoutSagaStep PendingStep(PayoutSaga saga) => saga.State switch
    {
        PayoutSagaState.Reserving => PayoutSagaStep.ReserveCommissions,
        PayoutSagaState.Crediting => PayoutSagaStep.CreditWallet,
        PayoutSagaState.Confirming => PayoutSagaStep.ConfirmPayment,
        PayoutSagaState.Compensating => PayoutSagaStep.ReleaseCommissions,
        _ => PayoutSagaStep.None,
    };

    // A reply is applied only in the state that waits for it; repeated and late replies return false.

    public bool OnCommissionsReserved(PayoutSaga saga, IReadOnlyList<PayoutSagaItem> items, DateTimeOffset now)
    {
        if (saga.State != PayoutSagaState.Reserving)
        {
            return false;
        }

        if (items.Count == 0)
        {
            Finish(saga, PayoutSagaState.NothingToPay, now, "No accrued commissions to pay out");
            return true;
        }

        saga.Items = items.ToList();
        saga.Amount = items.Sum(item => item.Amount);
        MoveTo(saga, PayoutSagaState.Crediting, now, $"Reserved {items.Count} commission(s) for {saga.Amount}: crediting the wallet");
        return true;
    }

    public bool OnWalletCredited(PayoutSaga saga, DateTimeOffset creditedAt, DateTimeOffset now)
    {
        if (saga.State != PayoutSagaState.Crediting)
        {
            return false;
        }

        saga.PaidAt = creditedAt;
        MoveTo(saga, PayoutSagaState.Confirming, now, "Wallet credited: marking the commissions as paid");
        return true;
    }

    public bool OnWalletCreditRejected(PayoutSaga saga, string reason, DateTimeOffset now)
    {
        if (saga.State != PayoutSagaState.Crediting)
        {
            return false;
        }

        saga.FailureReason = reason;
        MoveTo(saga, PayoutSagaState.Compensating, now, $"Wallet rejected the payout ({reason}): releasing the commissions");
        return true;
    }

    public bool OnPaymentConfirmed(PayoutSaga saga, DateTimeOffset now)
    {
        if (saga.State != PayoutSagaState.Confirming)
        {
            return false;
        }

        Finish(saga, PayoutSagaState.Completed, now, "Commissions marked as paid");
        return true;
    }

    public bool OnCommissionsReleased(PayoutSaga saga, DateTimeOffset now)
    {
        if (saga.State != PayoutSagaState.Compensating)
        {
            return false;
        }

        Finish(saga, PayoutSagaState.Compensated, now, "Commissions released: they stay accrued for a later payout");
        return true;
    }

    /// <summary>
    /// Called when no reply came in time: the command of the current step is sent again, each time after a
    /// longer pause. A timeout never cancels the payout: once the wallet was asked to credit, we do not know
    /// whether it did, and giving up could pay the same commissions twice.
    /// </summary>
    /// <returns>The step whose command must be sent again.</returns>
    public PayoutSagaStep OnStepTimeout(PayoutSaga saga, DateTimeOffset now)
    {
        if (!ActiveStates.Contains(saga.State))
        {
            return PayoutSagaStep.None;
        }

        saga.StepAttempts++;
        saga.UpdatedAt = now;
        var backoff = timing.StepTimeout * Math.Pow(2, Math.Min(saga.StepAttempts, 10));
        saga.NextAttemptAt = now + (backoff < timing.MaxRetryDelay ? backoff : timing.MaxRetryDelay);
        saga.History.Add(new PayoutSagaTransition(now, saga.State, saga.State,
            $"No reply in time: command re-sent (retry {saga.StepAttempts})"));

        return PendingStep(saga);
    }

    private void MoveTo(PayoutSaga saga, PayoutSagaState state, DateTimeOffset now, string note)
    {
        saga.History.Add(new PayoutSagaTransition(now, saga.State, state, note));
        saga.State = state;
        saga.StepAttempts = 0;
        saga.UpdatedAt = now;
        saga.NextAttemptAt = now + timing.StepTimeout;
    }

    private static void Finish(PayoutSaga saga, PayoutSagaState state, DateTimeOffset now, string note)
    {
        saga.History.Add(new PayoutSagaTransition(now, saga.State, state, note));
        saga.State = state;
        saga.UpdatedAt = now;
        saga.CompletedAt = now;
        saga.NextAttemptAt = null;
    }
}
