using System.Text.RegularExpressions;

namespace ValetaxTest.Core;

/// <summary>
/// Format of user and event ids that come from external systems: case-sensitive, safe for URLs,
/// starting with a letter or a digit.
/// </summary>
public static partial class ExternalIds
{
    public const int UserIdMaxLength = 64;

    public const int EventIdMaxLength = 128;

    public const string FormatMessage = "Only latin letters, digits and '.', '_', ':', '-' are allowed, starting with a letter or a digit.";

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._:-]*$", RegexOptions.CultureInvariant)]
    public static partial Regex Pattern();
}
