using FluentValidation;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using ValetaxTest.Core;
using ValetaxTest.Core.Application;
using ValetaxTest.Core.Persistence;

namespace ValetaxTest.ServiceDefaults;

/// <summary>Turns exceptions of gRPC methods into gRPC status codes.</summary>
public sealed class GrpcExceptionInterceptor(ILogger<GrpcExceptionInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (Exception exception) when (exception is not RpcException)
        {
            // The same mapping as in ApiExceptionHandler, only to gRPC codes.
            var status = exception switch
            {
                ValidationException validation => new Status(
                    StatusCode.InvalidArgument,
                    string.Join("; ", validation.Errors.Select(failure => $"{failure.PropertyName}: {failure.ErrorMessage}"))),
                NotFoundException => new Status(StatusCode.NotFound, exception.Message),
                ConflictException => new Status(StatusCode.AlreadyExists, exception.Message),
                PersistenceConflictException => new Status(StatusCode.Aborted, "The resource was modified concurrently, retry the call."),
                DomainException => new Status(StatusCode.FailedPrecondition, exception.Message),
                _ when TransientErrorDetector.IsTransient(exception) => new Status(StatusCode.Unavailable, "Temporarily unavailable"),
                _ => new Status(StatusCode.Internal, "Internal error"),
            };

            if (status.StatusCode is StatusCode.Internal or StatusCode.Unavailable)
            {
                logger.LogError(exception, "gRPC call {Method} failed", context.Method);
            }

            throw new RpcException(status);
        }
    }
}
