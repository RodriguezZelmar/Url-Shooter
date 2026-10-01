using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Application.Common;
using UrlShortener.Domain.Common;

namespace UrlShortener.Api.ErrorHandling;

/// <summary>
/// Translates exceptions thrown by the use cases into ProblemDetails responses (RFC 9457).
/// This is the only place that decides which status code each business error gets.
/// </summary>
internal sealed class ApiExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            RequestValidationException validation => new ValidationProblemDetails(
                validation.Errors.ToDictionary(e => e.Key, e => e.Value))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
            },
            DomainValidationException domain => Problem(StatusCodes.Status400BadRequest, "Invalid request.", domain.Message),
            ShortUrlNotFoundException notFound => Problem(StatusCodes.Status404NotFound, "Short URL not found.", notFound.Message),
            ShortCodeConflictException conflict => Problem(StatusCodes.Status409Conflict, "Short code already in use.", conflict.Message),
            _ => null,
        };

        if (problem is null)
        {
            // Unexpected: log the details, but never return them to the client.
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
            problem = Problem(StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails Problem(int status, string title, string? detail) =>
        new() { Status = status, Title = title, Detail = detail };
}
