using Microsoft.AspNetCore.Mvc;
using ValetaxTest.Partners.Application.Users;

namespace ValetaxTest.Partners.API.Controllers;

[ApiController]
[Route("api/users")]
[Tags("Users")]
public sealed class UsersController(IUserService users) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Registers a user")]
    [EndpointDescription("`partnerId` is the ExternalId of the user who invited the new user (level-1 partner); omit it when there is none.")]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var user = await users.RegisterAsync(request, cancellationToken);
        return Created($"/api/users/{user.ExternalId}", user);
    }

    [HttpGet]
    [EndpointSummary("Lists registered users, oldest first")]
    [ProducesResponseType<IReadOnlyList<UserDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IReadOnlyList<UserDto>> List(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        users.ListAsync(page, pageSize, cancellationToken);

    [HttpGet("{userId}")]
    [EndpointSummary("Gets a user")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<UserDto> Get(string userId, CancellationToken cancellationToken) =>
        users.GetAsync(userId, cancellationToken);

    [HttpPut("{userId}/partner")]
    [EndpointSummary("Sets the partner (referrer) of a user")]
    [EndpointDescription("Only future commissions are affected. A link that would create a cycle is rejected with 422.")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<UserDto> AssignPartner(string userId, AssignPartnerRequest request, CancellationToken cancellationToken) =>
        users.AssignPartnerAsync(userId, request, cancellationToken);

    [HttpDelete("{userId}/partner")]
    [EndpointSummary("Removes the partner link of a user")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<UserDto> RemovePartner(string userId, CancellationToken cancellationToken) =>
        users.RemovePartnerAsync(userId, cancellationToken);

    [HttpGet("{userId}/upline")]
    [EndpointSummary("Branch up: partners of the user by level (1 = direct partner)")]
    [ProducesResponseType<UplineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<UplineDto> GetUpline(string userId, int? depth, CancellationToken cancellationToken) =>
        users.GetUplineAsync(userId, depth, cancellationToken);

    [HttpGet("{userId}/downline")]
    [EndpointSummary("Branch down: users invited by the user, recursively")]
    [ProducesResponseType<DownlineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<DownlineDto> GetDownline(string userId, int? depth, CancellationToken cancellationToken) =>
        users.GetDownlineAsync(userId, depth, cancellationToken);
}
