using Microsoft.AspNetCore.Mvc;
using ValetaxTest.Commissions.Application.Commissions;
using ValetaxTest.Commissions.Domain.Accrual;

namespace ValetaxTest.Commissions.API.Controllers;

[ApiController]
[Route("api/commissions")]
[Tags("Commissions")]
public sealed class CommissionsController(ICommissionService commissions) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Commission ledger: accrued, reserved and paid commissions, newest first")]
    [ProducesResponseType<IReadOnlyList<CommissionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IReadOnlyList<CommissionDto>> List(
        string? partnerId,
        string? eventId,
        CommissionStatus? status,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        commissions.ListAsync(partnerId, eventId, status, page, pageSize, cancellationToken);
}
