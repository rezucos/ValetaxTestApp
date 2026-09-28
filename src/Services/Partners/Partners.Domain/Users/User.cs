namespace ValetaxTest.Partners.Domain.Users;

/// <summary>
/// User of the partner program. A user has at most one partner: the user who invited them.
/// The rules are in <see cref="PartnerTreeService"/>.
/// </summary>
public sealed class User
{
    public required string ExternalId { get; set; }

    /// <summary>External id of the direct (level 1) partner; <c>null</c> when the user has no partner.</summary>
    public string? PartnerId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
