using System.Diagnostics.Metrics;
using ValetaxTest.Commissions.Domain.Accrual;

namespace ValetaxTest.Commissions.Application;

public sealed class CommissionsMetrics
{
    public const string MeterName = "ValetaxTest.Commissions";

    private readonly Counter<long> _eventsProcessed;
    private readonly Counter<long> _commissionsAccrued;
    private readonly Counter<double> _amountAccrued;
    private readonly Counter<long> _commissionsPaid;
    private readonly Counter<long> _reservationsReleased;

    public CommissionsMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _eventsProcessed = meter.CreateCounter<long>(
            "valetaxtest.commissions.events_processed", "{event}", "Profit events processed by the accrual engine, by outcome");
        _commissionsAccrued = meter.CreateCounter<long>(
            "valetaxtest.commissions.accrued", "{commission}", "Commissions accrued, by schema");
        _amountAccrued = meter.CreateCounter<double>(
            "valetaxtest.commissions.accrued_amount", "{unit}", "Total amount of accrued commissions, by schema");
        _commissionsPaid = meter.CreateCounter<long>(
            "valetaxtest.commissions.paid", "{commission}", "Commissions paid out");
        _reservationsReleased = meter.CreateCounter<long>(
            "valetaxtest.commissions.reservations_released", "{payout}", "Payout reservations released by compensation");
    }

    public void Accrued(AccrualResult result)
    {
        _eventsProcessed.Add(1, new KeyValuePair<string, object?>("outcome", result.ProcessedEvent.Outcome.ToString()));
        foreach (var commission in result.Commissions)
        {
            var schema = new KeyValuePair<string, object?>("schema_type", commission.SchemaType.ToString());
            _commissionsAccrued.Add(1, schema);
            _amountAccrued.Add((double)commission.Amount, schema);
        }
    }

    public void Paid(int count) => _commissionsPaid.Add(count);

    public void Released() => _reservationsReleased.Add(1);
}
