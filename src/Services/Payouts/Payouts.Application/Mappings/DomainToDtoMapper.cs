using ValetaxTest.Payouts.Application.Sagas;
using ValetaxTest.Payouts.Domain.Sagas;

namespace ValetaxTest.Payouts.Application.Mappings;

public static class DomainToDtoMapper
{
    extension(PayoutSaga saga)
    {
        public PayoutSummaryDto ToSummaryDto() => new(
            saga.Id,
            saga.PartnerId,
            saga.State,
            saga.Amount,
            saga.Items.Count,
            saga.FailureReason,
            saga.CreatedAt,
            saga.UpdatedAt,
            saga.CompletedAt);

        public PayoutDetailsDto ToDetailsDto() => new(
            saga.Id,
            saga.PartnerId,
            saga.State,
            saga.Amount,
            saga.FailureReason,
            saga.PaidAt,
            saga.StepAttempts,
            saga.NextAttemptAt,
            saga.CreatedAt,
            saga.UpdatedAt,
            saga.CompletedAt,
            saga.Items.Select(item => item.ToDto()).ToList(),
            saga.History.Select(transition => transition.ToDto()).ToList());

        public StartedPayout ToStartedPayout() => new(saga.Id, saga.PartnerId);
    }

    extension(PayoutSagaItem item)
    {
        public PayoutSagaItemDto ToDto() => new(item.CommissionId, item.EventId, item.Level, item.Amount, item.SchemaType);
    }

    extension(PayoutSagaTransition transition)
    {
        public PayoutSagaTransitionDto ToDto() => new(transition.At, transition.From, transition.To, transition.Note);
    }
}
