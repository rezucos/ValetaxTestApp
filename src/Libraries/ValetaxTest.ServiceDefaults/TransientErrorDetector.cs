using System.Data.Common;
using Grpc.Core;
using Polly.CircuitBreaker;
using Polly.Timeout;
using ValetaxTest.Core.Persistence;

namespace ValetaxTest.ServiceDefaults;

/// <summary>
/// Tells a temporary error (a database or another service is unavailable, retrying later can help)
/// from an error in the data or the code (retrying the same input fails again).
/// </summary>
public static class TransientErrorDetector
{
    public static bool IsTransient(Exception exception) => exception switch
    {
        TimeoutException or TimeoutRejectedException or BrokenCircuitException or HttpRequestException => true,
        OperationCanceledException => true,
        ConcurrencyConflictException => true,
        DbException database => database.IsTransient,
        RpcException rpc => IsTransient(rpc.StatusCode),
        _ => exception.InnerException is { } inner && IsTransient(inner),
    };

    public static bool IsTransient(StatusCode statusCode) =>
        statusCode is StatusCode.Unavailable
            or StatusCode.DeadlineExceeded
            or StatusCode.ResourceExhausted
            or StatusCode.Aborted;
}
