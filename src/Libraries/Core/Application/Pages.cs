using FluentValidation;
using FluentValidation.Results;

namespace ValetaxTest.Core.Application;

/// <summary>Paging of lists: pages are numbered from 1 and hold at most <see cref="MaxSize"/> items.</summary>
public static class Pages
{
    public const int MaxSize = 100;

    /// <summary>Throws <see cref="ValidationException"/> (HTTP 400) when the page or its size is out of range.</summary>
    public static void Validate(int page, int pageSize)
    {
        List<ValidationFailure> failures = [];
        if (page < 1)
        {
            failures.Add(new ValidationFailure(nameof(page), "Page must be 1 or greater."));
        }

        if (pageSize is < 1 or > MaxSize)
        {
            failures.Add(new ValidationFailure(nameof(pageSize), $"Page size must be between 1 and {MaxSize}."));
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }
    }
}
