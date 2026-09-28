using FluentValidation;
using ValetaxTest.Core;

namespace ValetaxTest.Events.Application.ProfitEvents;

/// <param name="ExternalId">Event id in the external system; repeating a request with the same id does not create a second event.</param>
/// <param name="Profit">Profit (positive) or loss (negative).</param>
/// <param name="OccurredAt">When the event happened; defaults to the time it is received.</param>
public sealed record RegisterProfitEventRequest(string ExternalId, string UserId, decimal? Profit, DateTimeOffset? OccurredAt);

public sealed class RegisterProfitEventRequestValidator : AbstractValidator<RegisterProfitEventRequest>
{
    public RegisterProfitEventRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.ExternalId)
            .NotEmpty()
            .MaximumLength(ExternalIds.EventIdMaxLength)
            .Matches(ExternalIds.Pattern()).WithMessage(ExternalIds.FormatMessage);

        RuleFor(request => request.UserId)
            .NotEmpty()
            .MaximumLength(ExternalIds.UserIdMaxLength)
            .Matches(ExternalIds.Pattern()).WithMessage(ExternalIds.FormatMessage);

        RuleFor(request => request.Profit)
            .NotNull()
            .InclusiveBetween(-Money.MaxEventAmount, Money.MaxEventAmount)
            .Must(profit => Money.FitsScale(profit!.Value)).WithMessage($"At most {Money.Scale} fractional digits are allowed.");
    }
}
