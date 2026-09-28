using ValetaxTest.Partners.Application.Users;
using ValetaxTest.Partners.Domain.Users;

namespace ValetaxTest.Partners.Application.Mappings;

public static class DomainToDtoMapper
{
    extension(User user)
    {
        /// <param name="referralsCount">Number of users the user has invited directly.</param>
        public UserDto ToDto(int referralsCount) => new(user.ExternalId, user.PartnerId, referralsCount, user.CreatedAt, user.UpdatedAt);
    }
}
