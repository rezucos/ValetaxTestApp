using ValetaxTest.Partners.Application.LinkEvents;
using ValetaxTest.Partners.Domain.Users;

namespace ValetaxTest.Partners.Application.Mappings;

/// <summary>Builds messages sent by the service.</summary>
public static class DomainToContractMapper
{
    extension(User user)
    {
        public UserRegisteredLinkEvent ToContract() => new(user.ExternalId, user.PartnerId, user.CreatedAt);
    }
}
