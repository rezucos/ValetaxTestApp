using Polly.Registry;
using ValetaxTest.Commissions.API.Grpc;
using ValetaxTest.Payouts.Application.Abstractions;
using ValetaxTest.Payouts.Infrastructure.Mappings;

namespace ValetaxTest.Payouts.Infrastructure.Commissions;

/// <summary>Asks the Commissions service over gRPC which partners have accrued commissions.</summary>
public sealed class CommissionsLedgerGrpcAdapter(
    CommissionsLedger.CommissionsLedgerClient client,
    ResiliencePipelineProvider<string> pipelines) : ICommissionsLedger
{
    public const string ServiceName = "Commissions";

    public async Task<PartnersPage> ListPartnersWithAccruedCommissionsAsync(string? afterPartnerId, int pageSize, CancellationToken cancellationToken)
    {
        var request = new ListPartnersWithAccruedCommissionsRequest { PageSize = pageSize, AfterPartnerId = afterPartnerId ?? string.Empty };

        var reply = await pipelines.GetPipeline(ServiceName).ExecuteAsync(
            async token => await client.ListPartnersWithAccruedCommissionsAsync(request, cancellationToken: token),
            cancellationToken);

        return reply.ToDto();
    }
}
