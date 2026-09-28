using Microsoft.AspNetCore.Mvc;
using ValetaxTest.Events.Application.ProfitEvents;

namespace ValetaxTest.Events.API.Controllers;

[ApiController]
[Route("api/events")]
[Tags("Events")]
public sealed class EventsController(IEventService events) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Accepts a profit/loss event of a user")]
    [EndpointDescription(
        "Returns 202 when the event is accepted; commissions are calculated asynchronously. " +
        "Repeating a request with the same `externalId` is idempotent: the stored event is returned with 200 " +
        "(or 409 when the data differs).")]
    [ProducesResponseType<ProfitEventDto>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProfitEventDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Register(RegisterProfitEventRequest request, CancellationToken cancellationToken)
    {
        var result = await events.RegisterAsync(request, cancellationToken);
        if (result.Created)
        {
            return Accepted($"/api/events/{result.Event.ExternalId}", result.Event);
        }

        Response.Headers["Idempotent-Replayed"] = "true";
        return Ok(result.Event);
    }

    [HttpGet]
    [EndpointSummary("Lists events of a user (without commission details), newest first")]
    [ProducesResponseType<IReadOnlyList<ProfitEventDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IReadOnlyList<ProfitEventDto>> List(
        string userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        events.ListAsync(userId, page, pageSize, cancellationToken);

    [HttpGet("{eventId}")]
    [EndpointSummary("Gets an event with all commissions accrued for it")]
    [EndpointDescription("For every commission: the partner, level, amount, schema it was calculated with and whether it is paid out.")]
    [ProducesResponseType<ProfitEventDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ProfitEventDetailsDto> Get(string eventId, CancellationToken cancellationToken) =>
        events.GetAsync(eventId, cancellationToken);
}
