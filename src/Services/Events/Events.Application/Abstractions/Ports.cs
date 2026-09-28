using ValetaxTest.Events.Application.ProfitEvents;

namespace ValetaxTest.Events.Application.Abstractions;

/// <summary>Asks the Partners service whether a user is registered.</summary>
public interface IUserDirectory
{
    Task<bool> IsRegisteredAsync(string userId, CancellationToken cancellationToken);
}

/// <summary>Queries of profit events.</summary>
public interface IProfitEventReader
{
    /// <summary>Events of the user, newest first; pages start from 1.</summary>
    Task<IReadOnlyList<ProfitEventDto>> ListByUserAsync(string userId, int page, int pageSize, CancellationToken cancellationToken);

    Task<ProfitEventDetailsDto?> GetDetailsAsync(string externalId, CancellationToken cancellationToken);
}
