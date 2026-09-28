namespace ValetaxTest.Payouts.Application.Sagas.Messages;

/// <summary>A commission included in a payout.</summary>
public sealed record PayoutItem(Guid CommissionId, string EventId, int Level, decimal Amount, CommissionSchemaType SchemaType);
