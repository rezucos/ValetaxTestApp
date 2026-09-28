using ValetaxTest.Commissions.Domain.Accrual;
using ValetaxTest.Commissions.Domain.Schemas;

namespace ValetaxTest.Commissions.Application.Commissions;

public sealed record CommissionDto(
    Guid Id,
    string EventId,
    string SourceUserId,
    string PartnerId,
    int Level,
    int RatePercent,
    decimal Amount,
    CommissionSchemaType SchemaType,
    CommissionStatus Status,
    Guid? PayoutId,
    DateTimeOffset AccruedAt,
    DateTimeOffset? PaidAt);

public sealed record PartnersPage(IReadOnlyList<string> PartnerIds, bool HasMore);

public sealed record SchemaChangeDto(CommissionSchemaType PreviousSchemaType, CommissionSchemaType SchemaType, DateTimeOffset ChangedAt);

/// <param name="SchemaType">Schema applied to new calculations.</param>
/// <param name="History">Latest switches, newest first.</param>
public sealed record CommissionSchemaDto(CommissionSchemaType SchemaType, DateTimeOffset UpdatedAt, IReadOnlyList<SchemaChangeDto> History);
