using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using ValetaxTest.Core;
using ValetaxTest.Core.Application;
using ValetaxTest.Core.Persistence;
using ValetaxTest.EventBus.Abstractions;
using ValetaxTest.Events.Application.Abstractions;
using ValetaxTest.Events.Application.Mappings;
using ValetaxTest.Events.Domain.ProfitEvents;

namespace ValetaxTest.Events.Application.ProfitEvents;

/// <summary>
/// Accepts profit and loss events. The event and the message for the Commissions service are saved
/// in one transaction; commissions are calculated there later.
/// </summary>
public sealed class EventService(
    IProfitEventRepository events,
    IProfitEventReader reader,
    IUserDirectory userDirectory,
    ProfitEventService profitEventService,
    IValidator<RegisterProfitEventRequest> registerValidator,
    IOutbox outbox,
    IUnitOfWork unitOfWork,
    EventsMetrics metrics,
    TimeProvider timeProvider,
    ILogger<EventService> logger) : IEventService
{
    public async Task<RegisterProfitEventResult> RegisterAsync(RegisterProfitEventRequest request, CancellationToken cancellationToken)
    {
        await registerValidator.ValidateAndThrowAsync(request, cancellationToken);

        var profit = request.Profit!.Value;

        // A repeated request gets the stored event instead of creating a second one.
        var existing = await events.GetSnapshotAsync(request.ExternalId, cancellationToken);
        if (existing is not null)
        {
            if (!profitEventService.IsSameEvent(existing, request.UserId, profit, request.OccurredAt))
            {
                throw new ConflictException($"Event '{request.ExternalId}' is already registered with different data.");
            }

            metrics.EventRegistered(created: false);
            return new RegisterProfitEventResult(existing.ToDto(), Created: false);
        }

        if (!await userDirectory.IsRegisteredAsync(request.UserId, cancellationToken))
        {
            throw new DomainException($"User '{request.UserId}' is not registered in the partner program.");
        }

        var profitEvent = profitEventService.Register(request.ExternalId, request.UserId, profit, request.OccurredAt, timeProvider.GetUtcNow());
        events.Add(profitEvent);
        outbox.Enqueue(Topics.ProfitEventRegistered, profitEvent.ExternalId, profitEvent.ToContract(), correlationId: profitEvent.ExternalId);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateKeyException)
        {
            // Another request with the same ExternalId was saved first.
            var winner = await events.GetSnapshotAsync(request.ExternalId, cancellationToken)
                ?? throw new InvalidOperationException($"Event '{request.ExternalId}' disappeared after a duplicate key error.");
            if (!profitEventService.IsSameEvent(winner, request.UserId, profit, request.OccurredAt))
            {
                throw new ConflictException($"Event '{request.ExternalId}' is already registered with different data.");
            }

            metrics.EventRegistered(created: false);
            return new RegisterProfitEventResult(winner.ToDto(), Created: false);
        }

        metrics.EventRegistered(created: true);
        logger.LogInformation("Profit event {EventId} of user {UserId} registered: {Profit}",
            profitEvent.ExternalId, profitEvent.UserId, profitEvent.Profit);

        return new RegisterProfitEventResult(profitEvent.ToDto(), Created: true);
    }

    public Task<IReadOnlyList<ProfitEventDto>> ListAsync(string userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new ValidationException([new ValidationFailure(nameof(userId), "User id is required.")]);
        }

        Pages.Validate(page, pageSize);
        return reader.ListByUserAsync(userId, page, pageSize, cancellationToken);
    }

    public async Task<ProfitEventDetailsDto> GetAsync(string eventId, CancellationToken cancellationToken) =>
        await reader.GetDetailsAsync(eventId, cancellationToken)
        ?? throw new NotFoundException($"Event '{eventId}' is not registered.");
}
