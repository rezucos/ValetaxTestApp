namespace ValetaxTest.Core;

/// <summary>Amounts (profits, commissions, balances) in conventional units with up to <see cref="Scale"/> digits after the point.</summary>
public static class Money
{
    public const int Scale = 8;

    /// <summary>Total number of digits of a stored amount.</summary>
    public const int Precision = 24;

    /// <summary>Largest absolute profit accepted for a single event.</summary>
    public const decimal MaxEventAmount = 1_000_000_000_000m;

    /// <summary>Rounds to <see cref="Scale"/> digits; a half goes to the even digit (banker's rounding).</summary>
    public static decimal Round(decimal amount) => Math.Round(amount, Scale, MidpointRounding.ToEven);

    /// <summary>Whether the amount has at most <see cref="Scale"/> digits after the point.</summary>
    public static bool FitsScale(decimal amount) => decimal.Round(amount, Scale) == amount;
}
