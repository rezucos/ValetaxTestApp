using Microsoft.AspNetCore.Mvc;
using ValetaxTest.Wallets.Application.Wallets;

namespace ValetaxTest.Wallets.API.Controllers;

[ApiController]
[Route("api/wallets")]
[Tags("Wallets")]
public sealed class WalletsController(IWalletService wallets) : ControllerBase
{
    [HttpGet("{userId}")]
    [EndpointSummary("Current balance of the user's wallet (sum of paid-out commissions)")]
    [ProducesResponseType<WalletDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<WalletDto> Get(string userId, CancellationToken cancellationToken) =>
        wallets.GetAsync(userId, cancellationToken);

    [HttpGet("{userId}/payouts")]
    [EndpointSummary("Payout history: which commissions were paid out and when, newest first")]
    [ProducesResponseType<IReadOnlyList<WalletPayoutDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<WalletPayoutDto>> GetPayouts(
        string userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        wallets.ListPayoutsAsync(userId, page, pageSize, cancellationToken);
}
