namespace ValetaxTest.Events.Application;

/// <summary>Kafka topics the service writes to or reads from.</summary>
public static class Topics
{
    /// <summary><see cref="LinkEvents.ProfitEventRegisteredLinkEvent"/>, key = event id.</summary>
    public const string ProfitEventRegistered = "events.profit-event-registered.v1";

    /// <summary><see cref="LinkEvents.CommissionsCalculatedLinkEvent"/>, key = event id.</summary>
    public const string CommissionsCalculated = "commissions.commissions-calculated.v1";

    /// <summary><see cref="LinkEvents.CommissionsPaidLinkEvent"/>, key = payout id.</summary>
    public const string CommissionsPaid = "commissions.commissions-paid.v1";
}
