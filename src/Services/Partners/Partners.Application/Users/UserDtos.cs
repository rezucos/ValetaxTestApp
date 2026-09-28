namespace ValetaxTest.Partners.Application.Users;

/// <param name="PartnerId">Direct (level 1) partner, if any.</param>
/// <param name="ReferralsCount">Number of users this user has invited directly.</param>
public sealed record UserDto(
    string ExternalId,
    string? PartnerId,
    int ReferralsCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record UplinePartnerDto(int Level, string UserId);

/// <summary>Branch up: the user's partner (level 1), their partner (level 2) and so on.</summary>
public sealed record UplineDto(string UserId, int Depth, IReadOnlyList<UplinePartnerDto> Partners);

public sealed record DownlineNodeDto(string UserId, int Level, IReadOnlyList<DownlineNodeDto> Referrals);

/// <summary>Branch down: users invited by the user, the users invited by them and so on.</summary>
/// <param name="TotalReferrals">Number of users in the returned branch.</param>
/// <param name="Truncated">The branch was cut at <see cref="PartnerTreeOptions.MaxDownlineSize"/> users.</param>
public sealed record DownlineDto(string UserId, int Depth, int TotalReferrals, bool Truncated, IReadOnlyList<DownlineNodeDto> Referrals);
