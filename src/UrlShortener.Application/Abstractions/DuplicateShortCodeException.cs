namespace UrlShortener.Application.Abstractions;

/// <summary>
/// Raised by the persistence layer when the database's unique constraint on the short code rejects an insert.
/// This is how collisions that happen between the existence check and the insert (concurrent requests) are caught.
/// </summary>
public sealed class DuplicateShortCodeException(string shortCode, Exception? innerException = null)
    : Exception($"The short code '{shortCode}' already exists.", innerException)
{
    public string ShortCode { get; } = shortCode;
}
