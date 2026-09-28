using Grpc.Core;
using ValetaxTest.Partners.API.Mappings;
using ValetaxTest.Partners.Application.Users;

namespace ValetaxTest.Partners.API.Grpc;

/// <summary>gRPC API for other services: user lookup and the partner chain for commissions.</summary>
public sealed class PartnersDirectoryGrpcService(IUserService users) : PartnersDirectory.PartnersDirectoryBase
{
    public override async Task<UserReply> GetUser(GetUserRequest request, ServerCallContext context)
    {
        var user = await users.GetAsync(request.UserId, context.CancellationToken);
        return user.ToGrpc();
    }

    public override async Task<GetUplineReply> GetUpline(GetUplineRequest request, ServerCallContext context)
    {
        var upline = await users.GetUplineAsync(request.UserId, request.MaxDepth, context.CancellationToken);
        return upline.ToGrpc();
    }
}
