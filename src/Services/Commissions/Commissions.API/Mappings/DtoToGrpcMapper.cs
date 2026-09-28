using ValetaxTest.Commissions.API.Grpc;
using ValetaxTest.Commissions.Application.Commissions;

namespace ValetaxTest.Commissions.API.Mappings;

public static class DtoToGrpcMapper
{
    extension(PartnersPage page)
    {
        public ListPartnersWithAccruedCommissionsReply ToGrpc() => new()
        {
            PartnerIds = { page.PartnerIds },
            HasMore = page.HasMore,
        };
    }
}
