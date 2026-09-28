using Grpc.Core;
using ValetaxTest.Commissions.API.Mappings;
using ValetaxTest.Commissions.Application.Commissions;

namespace ValetaxTest.Commissions.API.Grpc;

/// <summary>gRPC API for the Payouts service.</summary>
public sealed class CommissionsLedgerGrpcService(ICommissionService commissions) : CommissionsLedger.CommissionsLedgerBase
{
    public override async Task<ListPartnersWithAccruedCommissionsReply> ListPartnersWithAccruedCommissions(
        ListPartnersWithAccruedCommissionsRequest request,
        ServerCallContext context)
    {
        // proto3 has no null: an empty after_partner_id means "from the beginning".
        var afterPartnerId = request.AfterPartnerId.Length == 0 ? null : request.AfterPartnerId;
        var page = await commissions.ListPartnersWithAccruedAsync(afterPartnerId, request.PageSize, context.CancellationToken);
        return page.ToGrpc();
    }
}
