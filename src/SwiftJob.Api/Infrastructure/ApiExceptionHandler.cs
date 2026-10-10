using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SwiftJob.Application.Common;

namespace SwiftJob.Api.Infrastructure;

/// <summary>Translates known application exceptions into RFC 7807 problem responses.</summary>
internal sealed class ApiExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request"),
            ConflictException => (StatusCodes.Status409Conflict, "Request conflicts with the current state"),
            ExternalServiceException => (StatusCodes.Status502BadGateway, "External service error"),
            OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested =>
                (499, "Request cancelled"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred"),
        };

        if (status == StatusCodes.Status502BadGateway)
        {
            logger.LogWarning(exception, "External service failure for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                // Only expose messages from exceptions we raise deliberately.
                Detail = status == StatusCodes.Status500InternalServerError ? null : exception.Message,
            },
        });
    }
}
