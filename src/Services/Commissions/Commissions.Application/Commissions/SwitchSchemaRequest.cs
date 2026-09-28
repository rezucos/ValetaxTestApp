using FluentValidation;
using ValetaxTest.Commissions.Domain.Schemas;

namespace ValetaxTest.Commissions.Application.Commissions;

/// <param name="SchemaType">"Linear" or "Fibonacci" (case-insensitive).</param>
public sealed record SwitchSchemaRequest(string? SchemaType);

public sealed class SwitchSchemaRequestValidator : AbstractValidator<SwitchSchemaRequest>
{
    public SwitchSchemaRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.SchemaType)
            .NotEmpty()
            .IsEnumName(typeof(CommissionSchemaType), caseSensitive: false)
            .WithMessage($"Must be one of: {string.Join(", ", Enum.GetNames<CommissionSchemaType>())}.");
    }
}
