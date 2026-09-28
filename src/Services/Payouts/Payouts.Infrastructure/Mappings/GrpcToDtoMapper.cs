using ValetaxTest.Commissions.API.Grpc;
using ValetaxTest.Payouts.Application.Abstractions;

namespace ValetaxTest.Payouts.Infrastructure.Mappings;

public static class GrpcToDtoMapper
{
    extension(ListPartnersWithAccruedCommissionsReply reply)
    {
        public PartnersPage ToDto() => new(reply.PartnerIds.ToList(), reply.HasMore);
    }
}
