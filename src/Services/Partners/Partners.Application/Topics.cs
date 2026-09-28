namespace ValetaxTest.Partners.Application;

/// <summary>Kafka topics the service writes to or reads from.</summary>
public static class Topics
{
    /// <summary><see cref="LinkEvents.UserRegisteredLinkEvent"/>, key = user id.</summary>
    public const string UserRegistered = "partners.user-registered.v1";
}
