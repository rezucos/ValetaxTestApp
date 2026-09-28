using ValetaxTest.Partners.API.Grpc;
using ValetaxTest.Partners.Application.Users;

namespace ValetaxTest.Partners.API.Mappings;

public static class DtoToGrpcMapper
{
    extension(UserDto user)
    {
        public UserReply ToGrpc()
        {
            var reply = new UserReply { UserId = user.ExternalId };

            // proto3 has no null: partner_id stays unset when the user has no partner.
            if (user.PartnerId is not null)
            {
                reply.PartnerId = user.PartnerId;
            }

            return reply;
        }
    }

    extension(UplineDto upline)
    {
        public GetUplineReply ToGrpc() => new()
        {
            UserId = upline.UserId,
            Partners = { upline.Partners.Select(partner => partner.ToGrpc()) },
        };
    }

    extension(UplinePartnerDto partner)
    {
        public UplinePartner ToGrpc() => new() { Level = partner.Level, UserId = partner.UserId };
    }
}
