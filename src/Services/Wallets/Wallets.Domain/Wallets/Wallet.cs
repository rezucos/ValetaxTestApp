namespace ValetaxTest.Wallets.Domain.Wallets;

/// <summary>
/// Internal wallet of a user in conventional units. Only paid commissions reach it: the balance is the sum
/// of credited payouts. The rules are in <see cref="WalletRules"/>.
/// </summary>
public sealed class Wallet
{
    /// <summary>ExternalId of the owner (one wallet per user).</summary>
    public required string UserId { get; set; }

    public decimal Balance { get; set; }

    public WalletStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

public enum WalletStatus
{
    Active = 1,

    /// <summary>Payouts to the wallet are rejected (e.g. during an investigation).</summary>
    Blocked = 2,
}

/// <summary>Result of a payout command, stored by payout id.</summary>
public sealed class WalletPayout
{
    public Guid PayoutId { get; set; }

    public required string UserId { get; set; }

    public WalletPayoutStatus Status { get; set; }

    /// <summary>Credited amount; zero for a rejected payout.</summary>
    public decimal Amount { get; set; }

    public string? RejectionReason { get; set; }

    public DateTimeOffset ProcessedAt { get; set; }

    /// <summary>Commissions paid out by this payout (empty for a rejected one).</summary>
    public List<WalletPayoutItem> Items { get; set; } = [];
}

public enum WalletPayoutStatus
{
    Credited = 1,
    Rejected = 2,
}

/// <summary>A commission paid out to the wallet. A commission can be paid out only once.</summary>
public sealed class WalletPayoutItem
{
    public Guid CommissionId { get; set; }

    public Guid PayoutId { get; set; }

    public required string EventId { get; set; }

    public int Level { get; set; }

    public decimal Amount { get; set; }

    public CommissionSchemaType SchemaType { get; set; }
}

public enum CommissionSchemaType
{
    Linear = 1,
    Fibonacci = 2,
}
