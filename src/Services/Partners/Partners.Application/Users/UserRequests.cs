using FluentValidation;
using ValetaxTest.Core;

namespace ValetaxTest.Partners.Application.Users;

/// <param name="PartnerId">The user who invited the new user; <c>null</c> if nobody did.</param>
public sealed record RegisterUserRequest(string ExternalId, string? PartnerId);

public sealed record AssignPartnerRequest(string PartnerId);

public sealed class RegisterUserRequestValidator : AbstractValidator<RegisterUserRequest>
{
    public RegisterUserRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.ExternalId)
            .NotEmpty()
            .MaximumLength(ExternalIds.UserIdMaxLength)
            .Matches(ExternalIds.Pattern()).WithMessage(ExternalIds.FormatMessage);

        // Optional: a user may register without a partner.
        RuleFor(request => request.PartnerId)
            .MaximumLength(ExternalIds.UserIdMaxLength)
            .Matches(ExternalIds.Pattern()).WithMessage(ExternalIds.FormatMessage);
    }
}

public sealed class AssignPartnerRequestValidator : AbstractValidator<AssignPartnerRequest>
{
    public AssignPartnerRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.PartnerId)
            .NotEmpty()
            .MaximumLength(ExternalIds.UserIdMaxLength)
            .Matches(ExternalIds.Pattern()).WithMessage(ExternalIds.FormatMessage);
    }
}
