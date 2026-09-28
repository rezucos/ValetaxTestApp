using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ValetaxTest.Core;
using ValetaxTest.Core.Application;
using ValetaxTest.Core.Persistence;

namespace ValetaxTest.ServiceDefaults;

/// <summary>
/// Turns known exceptions into HTTP errors with problem details. Other exceptions go to the default
/// handler, which logs them and returns 500 without details.
/// </summary>
public sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            // The client has disconnected, nobody is waiting for the answer.
            httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            return true;
        }

        ProblemDetails? problem = exception switch
        {
            // Field names in camelCase, as they are written in the request JSON.
            ValidationException validation => new HttpValidationProblemDetails(validation.Errors
                .GroupBy(failure => JsonNamingPolicy.CamelCase.ConvertName(failure.PropertyName))
                .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
            },
            NotFoundException => Problem(StatusCodes.Status404NotFound, "Resource not found", exception.Message),
            ConflictException => Problem(StatusCodes.Status409Conflict, "Conflict", exception.Message),
            PersistenceConflictException => Problem(
                StatusCodes.Status409Conflict,
                "Concurrent modification",
                "The resource was modified concurrently, retry the request."),
            DomainException => Problem(StatusCodes.Status422UnprocessableEntity, "Business rule violation", exception.Message),
            _ when TransientErrorDetector.IsTransient(exception) => Problem(
                StatusCodes.Status503ServiceUnavailable,
                "Service temporarily unavailable",
                "A dependency is temporarily unavailable, retry the request later."),
            _ => null,
        };

        if (problem is null)
        {
            return false;
        }

        if (problem.Status == StatusCodes.Status503ServiceUnavailable)
        {
            logger.LogWarning(exception, "Request {Method} {Path} failed because a dependency is unavailable",
                httpContext.Request.Method, httpContext.Request.Path);
            httpContext.Response.Headers.RetryAfter = "5";
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails Problem(int status, string title, string detail) =>
        new() { Status = status, Title = title, Detail = detail };
}
