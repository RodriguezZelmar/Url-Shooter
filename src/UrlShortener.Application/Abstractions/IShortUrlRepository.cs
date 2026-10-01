using UrlShortener.Domain.ShortUrls;

namespace UrlShortener.Application.Abstractions;

/// <summary>Persistence of <see cref="ShortUrl"/>. Implemented with EF Core in the Infrastructure layer.</summary>
public interface IShortUrlRepository
{
    Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default);

    Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken cancellationToken = default);

    /// <summary>Inserts the link and saves it immediately.</summary>
    /// <exception cref="DuplicateShortCodeException">The short code is already used by another link.</exception>
    Task AddAsync(ShortUrl shortUrl, CancellationToken cancellationToken = default);
}
