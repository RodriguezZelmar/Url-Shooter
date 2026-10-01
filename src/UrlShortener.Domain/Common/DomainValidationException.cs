namespace UrlShortener.Domain.Common;

/// <summary>
/// Thrown when an entity would be created or changed into an invalid state.
/// The application layer validates input first, so reaching this exception means a caller skipped validation.
/// </summary>
public sealed class DomainValidationException(string message) : Exception(message);
