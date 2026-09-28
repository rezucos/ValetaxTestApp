namespace ValetaxTest.Events.Application.ProfitEvents;

/// <summary>Profit/loss events of users.</summary>
public interface IEventService
{
    /// <summary>
    /// Accepts an event; the Commissions service calculates its commissions later. A repeated request
    /// gets the stored event back with <see cref="RegisterProfitEventResult.Created"/> = <c>false</c>.
    /// </summary>
    Task<RegisterProfitEventResult> RegisterAsync(RegisterProfitEventRequest request, CancellationToken cancellationToken);

    /// <summary>Events of the user without commission details, newest first; pages start from 1.</summary>
    Task<IReadOnlyList<ProfitEventDto>> ListAsync(string userId, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Event with all commissions accrued for it.</summary>
    Task<ProfitEventDetailsDto> GetAsync(string eventId, CancellationToken cancellationToken);
}
