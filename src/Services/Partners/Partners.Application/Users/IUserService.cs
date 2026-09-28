namespace ValetaxTest.Partners.Application.Users;

/// <summary>Users of the partner program and the partner tree they form.</summary>
public interface IUserService
{
    Task<UserDto> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken);

    /// <summary>Users in order of registration; pages start from 1.</summary>
    Task<IReadOnlyList<UserDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<UserDto> GetAsync(string userId, CancellationToken cancellationToken);

    /// <summary>Sets the partner of the user. Only commissions calculated after the change use the new link.</summary>
    Task<UserDto> AssignPartnerAsync(string userId, AssignPartnerRequest request, CancellationToken cancellationToken);

    Task<UserDto> RemovePartnerAsync(string userId, CancellationToken cancellationToken);

    /// <param name="depth">Number of levels; <c>null</c> means <see cref="PartnerTreeOptions.MaxDepth"/>.</param>
    Task<UplineDto> GetUplineAsync(string userId, int? depth, CancellationToken cancellationToken);

    /// <param name="depth">Number of levels; <c>null</c> means <see cref="PartnerTreeOptions.MaxDepth"/>.</param>
    Task<DownlineDto> GetDownlineAsync(string userId, int? depth, CancellationToken cancellationToken);
}
