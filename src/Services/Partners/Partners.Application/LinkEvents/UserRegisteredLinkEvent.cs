// Each service that sends or receives this message has its own copy of this file:
// the class and property names must be the same in all copies.

namespace ValetaxTest.Partners.Application.LinkEvents;

/// <summary>A user has registered. Wallets opens a wallet for them.</summary>
public sealed record UserRegisteredLinkEvent(string UserId, string? PartnerId, DateTimeOffset RegisteredAt);
