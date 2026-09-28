namespace ValetaxTest.Commissions.Application;

public sealed class CommissionOptions
{
    public const string SectionName = "Commissions";

    /// <summary>Depth of the partner chain that receives commissions for an event.</summary>
    public int MaxLevels { get; set; } = 10;
}
