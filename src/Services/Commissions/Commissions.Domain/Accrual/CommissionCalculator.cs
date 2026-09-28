using ValetaxTest.Commissions.Domain.Schemas;
using ValetaxTest.Core;

namespace ValetaxTest.Commissions.Domain.Accrual;

/// <summary>Commission of one partner for one event.</summary>
public sealed record CommissionShare(string PartnerId, int Level, int RatePercent, decimal Amount);

/// <summary>
/// Calculates commissions of the partners above the event owner. Only a positive profit gives commissions;
/// the partner at level L gets <c>rate(L) * Profit / 100</c>, rounded to <see cref="Money.Scale"/> digits.
/// </summary>
public sealed class CommissionCalculator
{
    /// <param name="upline">Partners above the owner by level: index 0 is the direct partner (level 1).</param>
    public IReadOnlyList<CommissionShare> Calculate(decimal profit, IReadOnlyList<string> upline, ICommissionSchema schema, int maxLevels)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLevels, 1);

        if (profit <= 0)
        {
            return [];
        }

        var levels = Math.Min(upline.Count, maxLevels);
        var shares = new List<CommissionShare>(levels);
        for (var index = 0; index < levels; index++)
        {
            var level = index + 1;
            var rate = schema.GetRatePercent(level);
            var amount = Money.Round(profit * rate / 100m);

            // A very small profit can give a commission that rounds to zero; such a commission is not accrued.
            if (amount > 0)
            {
                shares.Add(new CommissionShare(upline[index], level, rate, amount));
            }
        }

        return shares;
    }
}
