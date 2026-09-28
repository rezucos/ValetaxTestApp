using Grpc.Core;
using Microsoft.Extensions.Caching.Memory;
using Polly.Registry;
using ValetaxTest.Events.Application.Abstractions;
using ValetaxTest.Partners.API.Grpc;

namespace ValetaxTest.Events.Infrastructure.Partners;

/// <summary>Asks the Partners service over gRPC whether a user is registered.</summary>
public sealed class UserDirectoryGrpcAdapter(
    PartnersDirectory.PartnersDirectoryClient client,
    ResiliencePipelineProvider<string> pipelines,
    IMemoryCache cache) : IUserDirectory
{
    public const string ServiceName = "Partners";

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<bool> IsRegisteredAsync(string userId, CancellationToken cancellationToken)
    {
        var cacheKey = $"partners:registered:{userId}";
        if (cache.TryGetValue(cacheKey, out _))
        {
            return true;
        }

        try
        {
            await pipelines.GetPipeline(ServiceName).ExecuteAsync(
                async token => await client.GetUserAsync(new GetUserRequest { UserId = userId }, cancellationToken: token),
                cancellationToken);
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
        {
            return false;
        }

        // Users are never removed from the program, so positive answers can be cached.
        cache.Set(cacheKey, true, CacheDuration);
        return true;
    }
}
