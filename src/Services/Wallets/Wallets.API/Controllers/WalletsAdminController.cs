using Microsoft.AspNetCore.Mvc;
using ValetaxTest.Wallets.Application.Wallets;

namespace ValetaxTest.Wallets.API.Controllers;

[ApiController]
[Route("api/admin/wallets")]
[Tags("Administration")]
public sealed class WalletsAdminController(IWalletService wallets) : ControllerBase
{
    [HttpPost("{userId}/block")]
    [EndpointSummary("Blocks payouts to the wallet")]
    [EndpointDescription("Payouts to a blocked wallet are rejected: the payout saga compensates and the commissions stay accrued until the next payout.")]
    [ProducesResponseType<WalletDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<WalletDto> Block(string userId, CancellationToken cancellationToken) =>
        wallets.BlockAsync(userId, cancellationToken);

    [HttpPost("{userId}/unblock")]
    [EndpointSummary("Unblocks payouts to the wallet")]
    [ProducesResponseType<WalletDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<WalletDto> Unblock(string userId, CancellationToken cancellationToken) =>
        wallets.UnblockAsync(userId, cancellationToken);
}
