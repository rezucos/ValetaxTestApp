using ValetaxTest.Commissions.Application.Commissions;
using ValetaxTest.Commissions.Domain.Accrual;
using ValetaxTest.Commissions.Domain.Schemas;

namespace ValetaxTest.Commissions.Application.Mappings;

public static class DomainToDtoMapper
{
    extension(Commission commission)
    {
        public CommissionDto ToDto() => new(
            commission.Id,
            commission.EventId,
            commission.SourceUserId,
            commission.PartnerId,
            commission.Level,
            commission.RatePercent,
            commission.Amount,
            commission.SchemaType,
            commission.Status,
            commission.PayoutId,
            commission.AccruedAt,
            commission.PaidAt);
    }

    extension(CommissionSettings settings)
    {
        /// <param name="history">Latest switches, newest first.</param>
        public CommissionSchemaDto ToDto(IReadOnlyList<SchemaChangeDto> history) => new(settings.SchemaType, settings.UpdatedAt, history);
    }

    extension(SchemaChange change)
    {
        public SchemaChangeDto ToDto() => new(change.PreviousSchemaType, change.SchemaType, change.ChangedAt);
    }
}
