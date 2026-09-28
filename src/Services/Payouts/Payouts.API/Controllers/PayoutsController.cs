using Microsoft.AspNetCore.Mvc;
using ValetaxTest.Payouts.Application.Sagas;
using ValetaxTest.Payouts.Domain.Sagas;

namespace ValetaxTest.Payouts.API.Controllers;

[ApiController]
[Route("api/payouts")]
[Tags("Payouts")]
public sealed class PayoutsController(IPayoutService payouts) : ControllerBase
{
    [HttpPost("run")]
    [EndpointSummary("Starts a payout run now (the same as the periodic one)")]
    [EndpointDescription(
        "Starts a payout saga for every partner with accrued commissions. The sagas run asynchronously: " +
        "follow them with GET /api/payouts. Partners whose payout is still in progress are skipped.")]
    [ProducesResponseType<PayoutRunResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public Task<PayoutRunResult> Run(CancellationToken cancellationToken) =>
        payouts.RunAsync(cancellationToken);

    [HttpGet]
    [EndpointSummary("Lists payouts (saga instances), newest first")]
    [ProducesResponseType<IReadOnlyList<PayoutSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IReadOnlyList<PayoutSummaryDto>> List(
        string? partnerId,
        PayoutSagaState? state,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        payouts.ListAsync(partnerId, state, page, pageSize, cancellationToken);

    [HttpGet("{payoutId:guid}")]
    [EndpointSummary("Gets a payout with its commissions and the history of saga transitions")]
    [ProducesResponseType<PayoutDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<PayoutDetailsDto> Get(Guid payoutId, CancellationToken cancellationToken) =>
        payouts.GetAsync(payoutId, cancellationToken);
}
