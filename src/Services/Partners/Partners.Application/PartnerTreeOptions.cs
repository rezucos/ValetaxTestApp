namespace ValetaxTest.Partners.Application;

public sealed class PartnerTreeOptions
{
    public const string SectionName = "PartnerTree";

    /// <summary>Maximum depth of a branch returned by queries; the chain of partners for commissions cannot be deeper either.</summary>
    public int MaxDepth { get; set; } = 10;

    /// <summary>Maximum number of users in one branch down; the deepest levels are cut when there are more.</summary>
    public int MaxDownlineSize { get; set; } = 1000;
}

/// <summary>Keys of the PostgreSQL advisory locks of the service.</summary>
internal static class LockKeys
{
    /// <summary>Partner links are changed one at a time, so two links made at once cannot create a cycle.</summary>
    public const long PartnerTree = 1001;
}
