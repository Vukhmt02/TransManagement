using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TransManagement.Application.Common.Exceptions;
using TransManagement.Domain.Exceptions;

namespace TransManagement.API.Middleware;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, code) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed", "validation_error"),
            DomainException domainException => (StatusCodes.Status400BadRequest, "Business rule violation", domainException.Code),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found", "not_found"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request", "invalid_request"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", "server_error")
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "An unhandled exception occurred while processing the request.");
        }
        else
        {
            logger.LogWarning(exception, "A request failed with error code {ErrorCode}.", code);
        }

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status == StatusCodes.Status500InternalServerError && !environment.IsDevelopment()
                ? "Please contact support if the problem persists."
                : exception.Message,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["code"] = code;
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors;
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}

