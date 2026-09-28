using ValetaxTest.Wallets.Domain.Wallets;

namespace ValetaxTest.Wallets.Application.Wallets;

/// <param name="Balance">Sum of the commissions paid out to the wallet.</param>
public sealed record WalletDto(string UserId, decimal Balance, WalletStatus Status, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

/// <summary>A payout: which commissions were paid out and when.</summary>
public sealed record WalletPayoutDto(Guid PayoutId, decimal Amount, DateTimeOffset PaidAt, IReadOnlyList<WalletPayoutItemDto> Commissions);

public sealed record WalletPayoutItemDto(Guid CommissionId, string EventId, int Level, decimal Amount, CommissionSchemaType SchemaType);
