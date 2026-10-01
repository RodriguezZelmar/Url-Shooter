namespace UrlShortener.Application.Common;

// Exceptions raised by the use cases. They describe *what* went wrong in business terms; the API layer
// decides how each one is represented over HTTP (status code + ProblemDetails).

/// <summary>Input broke one or more business rules. Maps to 400 Bad Request.</summary>
public sealed class RequestValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("One or more validation errors occurred.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

/// <summary>No link exists with the requested short code. Maps to 404 Not Found.</summary>
public sealed class ShortUrlNotFoundException(string shortCode)
    : Exception($"No short URL exists with code '{shortCode}'.")
{
    public string ShortCode { get; } = shortCode;
}

/// <summary>A custom short code requested by the client is already taken. Maps to 409 Conflict.</summary>
public sealed class ShortCodeConflictException(string shortCode)
    : Exception($"The short code '{shortCode}' is already in use.")
{
    public string ShortCode { get; } = shortCode;
}

/// <summary>
/// Every generated code collided with an existing one. With a sensible code length this means the code
/// space is close to exhausted, so it is a server-side problem (500), not something the client can fix.
/// </summary>
public sealed class ShortCodeGenerationException(int attempts)
    : Exception($"Could not generate a unique short code after {attempts} attempts.");
