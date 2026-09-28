using Grpc.Core;
using Polly.Registry;
using ValetaxTest.Commissions.Application.Abstractions;
using ValetaxTest.Partners.API.Grpc;

namespace ValetaxTest.Commissions.Infrastructure.Partners;

/// <summary>
/// Gets the partner chain of a user from the Partners service over gRPC at the moment of calculation,
/// so a partner link set a second ago is already taken into account. While Partners is down, the event
/// stays in Kafka and is handled later.
/// </summary>
public sealed class PartnerHierarchyGrpcAdapter(
    PartnersDirectory.PartnersDirectoryClient client,
    ResiliencePipelineProvider<string> pipelines) : IPartnerHierarchy
{
    public const string ServiceName = "Partners";

    public async Task<IReadOnlyList<string>?> GetUplineAsync(string userId, int maxLevels, CancellationToken cancellationToken)
    {
        GetUplineReply reply;
        try
        {
            reply = await pipelines.GetPipeline(ServiceName).ExecuteAsync(
                async token => await client.GetUplineAsync(new GetUplineRequest { UserId = userId, MaxDepth = maxLevels }, cancellationToken: token),
                cancellationToken);
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
        {
            return null;
        }

        var partners = reply.Partners.OrderBy(partner => partner.Level).ToList();
        for (var index = 0; index < partners.Count; index++)
        {
            if (partners[index].Level != index + 1)
            {
                throw new InvalidOperationException($"Upline of user '{userId}' has a gap at level {index + 1}.");
            }
        }

        return partners.Select(partner => partner.UserId).ToList();
    }
}
