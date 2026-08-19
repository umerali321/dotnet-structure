using System.Net;
using SkillsetsBackend.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace SkillsetsBackend.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug("Request cancelled by the client: {Path}", context.Request.Path);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            NotFoundException => (HttpStatusCode.NotFound, "Resource not found"),
            ValidationException => (HttpStatusCode.BadRequest, "Validation failed"),
            AuthenticationFailedException => (HttpStatusCode.Unauthorized, "Authentication failed"),
            AccountLockedException => (HttpStatusCode.Locked, "Account temporarily locked"),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "Access denied"),
            ConflictException => (HttpStatusCode.Conflict, "Conflict"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred"),
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred requestId={RequestId} path={Path}", context.TraceIdentifier, context.Request.Path);
        }
        else
        {
            _logger.LogWarning(
                exception, "Request failed with {StatusCode} requestId={RequestId} path={Path}",
                statusCode, context.TraceIdentifier, context.Request.Path);
        }

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path,
        };
        // Lets the frontend surface this in an error toast/console log so a user's bug report can be
        // correlated back to the exact server-side log lines above, without exposing anything sensitive.
        problemDetails.Extensions["requestId"] = context.TraceIdentifier;

        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors;
        }

        if (exception is AuthenticationFailedException { FailedAttemptCount: not null } authException)
        {
            problemDetails.Extensions["failedAttemptCount"] = authException.FailedAttemptCount;
            problemDetails.Extensions["remainingAttempts"] = authException.RemainingAttempts;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
