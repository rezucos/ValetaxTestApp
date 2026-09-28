using ValetaxTest.Commissions.Application.LinkEvents;

namespace ValetaxTest.Commissions.Application.Payouts.Messages;

/// <summary>A commission included in a payout.</summary>
public sealed record PayoutItem(Guid CommissionId, string EventId, int Level, decimal Amount, CommissionSchemaType SchemaType);
