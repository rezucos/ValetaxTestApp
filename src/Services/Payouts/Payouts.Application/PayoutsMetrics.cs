using System.Diagnostics.Metrics;
using ValetaxTest.Payouts.Domain.Sagas;

namespace ValetaxTest.Payouts.Application;

public sealed class PayoutsMetrics
{
    public const string MeterName = "ValetaxTest.Payouts";

    private readonly Counter<long> _started;
    private readonly Counter<long> _finished;
    private readonly Histogram<double> _duration;
    private readonly Counter<double> _paidAmount;
    private readonly Counter<long> _stepRetries;

    public PayoutsMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _started = meter.CreateCounter<long>("valetaxtest.payouts.started", "{payout}", "Payout sagas started");
        _finished = meter.CreateCounter<long>("valetaxtest.payouts.finished", "{payout}", "Payout sagas finished, by final state");
        _duration = meter.CreateHistogram<double>("valetaxtest.payouts.duration", "s", "Duration of payout sagas, by final state");
        _paidAmount = meter.CreateCounter<double>("valetaxtest.payouts.paid_amount", "{unit}", "Amount paid out by completed payouts");
        _stepRetries = meter.CreateCounter<long>("valetaxtest.payouts.step_retries", "{command}", "Saga commands re-sent after a timeout, by step");
    }

    public void SagasStarted(int count)
    {
        if (count > 0)
        {
            _started.Add(count);
        }
    }

    public void Transitioned(PayoutSaga saga)
    {
        if (saga.CompletedAt is not { } completedAt || PayoutSagaStateMachine.ActiveStates.Contains(saga.State))
        {
            return;
        }

        var state = new KeyValuePair<string, object?>("final_state", saga.State.ToString());
        _finished.Add(1, state);
        _duration.Record((completedAt - saga.CreatedAt).TotalSeconds, state);
        if (saga.State == PayoutSagaState.Completed)
        {
            _paidAmount.Add((double)saga.Amount);
        }
    }

    public void StepRetried(PayoutSagaStep step) =>
        _stepRetries.Add(1, new KeyValuePair<string, object?>("step", step.ToString()));
}
