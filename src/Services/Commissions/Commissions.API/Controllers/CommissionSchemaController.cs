using Microsoft.AspNetCore.Mvc;
using ValetaxTest.Commissions.Application.Commissions;

namespace ValetaxTest.Commissions.API.Controllers;

[ApiController]
[Route("api/admin/commission-schema")]
[Tags("Administration")]
public sealed class CommissionSchemaController(ICommissionService commissions) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Gets the commission schema applied to new calculations and the history of switches")]
    [ProducesResponseType<CommissionSchemaDto>(StatusCodes.Status200OK)]
    public Task<CommissionSchemaDto> Get(CancellationToken cancellationToken) =>
        commissions.GetSchemaAsync(cancellationToken);

    [HttpPut]
    [EndpointSummary("Switches the commission schema (Linear <-> Fibonacci)")]
    [EndpointDescription("Affects only commissions calculated after the switch; accrued commissions keep their schema_type.")]
    [ProducesResponseType<CommissionSchemaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<CommissionSchemaDto> Switch(SwitchSchemaRequest request, CancellationToken cancellationToken) =>
        commissions.SwitchSchemaAsync(request, cancellationToken);
}
