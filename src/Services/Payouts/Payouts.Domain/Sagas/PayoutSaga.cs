namespace ValetaxTest.Payouts.Domain.Sagas;

/// <summary>
/// One payout of accrued commissions to one partner. Its state is changed by <see cref="PayoutSagaStateMachine"/>.
/// </summary>
public sealed class PayoutSaga
{
    /// <summary>Payout id; Commissions and Wallets use it to recognize a repeated command.</summary>
    public Guid Id { get; set; }

    /// <summary>Partner receiving the payout (owner of the wallet).</summary>
    public required string PartnerId { get; set; }

    public PayoutSagaState State { get; set; }

    public decimal Amount { get; set; }

    /// <summary>Commissions reserved for this payout.</summary>
    public List<PayoutSagaItem> Items { get; set; } = [];

    /// <summary>When the wallet was credited.</summary>
    public DateTimeOffset? PaidAt { get; set; }

    /// <summary>Why the wallet rejected the payout.</summary>
    public string? FailureReason { get; set; }

    /// <summary>How many times the command of the current step has been re-sent.</summary>
    public int StepAttempts { get; set; }

    /// <summary>When the command of the current step is re-sent if no reply arrives; <c>null</c> once finished.</summary>
    public DateTimeOffset? NextAttemptAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>History of state changes.</summary>
    public List<PayoutSagaTransition> History { get; set; } = [];
}

public enum PayoutSagaState
{
    /// <summary>Waiting for Commissions to reserve the accrued commissions.</summary>
    Reserving = 1,

    /// <summary>Waiting for Wallets to credit the wallet.</summary>
    Crediting = 2,

    /// <summary>Waiting for Commissions to mark the commissions as paid.</summary>
    Confirming = 3,

    /// <summary>The wallet rejected the payout: waiting for Commissions to release the reservation.</summary>
    Compensating = 4,

    /// <summary>Final: the wallet is credited and the commissions are paid.</summary>
    Completed = 5,

    /// <summary>Final: the payout was rejected; the commissions are accrued again.</summary>
    Compensated = 6,

    /// <summary>Final: there was nothing to pay out.</summary>
    NothingToPay = 7,
}

/// <summary>Command the saga sends for its current state.</summary>
public enum PayoutSagaStep
{
    None = 0,
    ReserveCommissions = 1,
    CreditWallet = 2,
    ConfirmPayment = 3,
    ReleaseCommissions = 4,
}

public enum CommissionSchemaType
{
    Linear = 1,
    Fibonacci = 2,
}

public sealed record PayoutSagaItem(Guid CommissionId, string EventId, int Level, decimal Amount, CommissionSchemaType SchemaType);

public sealed record PayoutSagaTransition(DateTimeOffset At, PayoutSagaState? From, PayoutSagaState To, string Note);

public interface IPayoutSagaRepository
{
    /// <summary>Loads the saga to change it; the row stays locked until the transaction ends.</summary>
    Task<PayoutSaga?> FindForUpdateAsync(Guid payoutId, CancellationToken cancellationToken);

    /// <summary>Active sagas that got no reply in time, locked until the transaction ends; rows locked by others are skipped.</summary>
    Task<IReadOnlyList<PayoutSaga>> LockTimedOutAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken);

    /// <summary>Which of the given partners already have a payout in progress.</summary>
    Task<IReadOnlySet<string>> GetPartnersWithActiveSagasAsync(IReadOnlyCollection<string> partnerIds, CancellationToken cancellationToken);

    void Add(PayoutSaga saga);
}
