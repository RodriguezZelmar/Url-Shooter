using UrlShortener.Application.ShortUrls;
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

    /// <summary>Saves changes made to links loaded through this repository.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns one page of links matching the filters, newest first, plus the total match count.</summary>
    /// <param name="query">Filters and paging, already validated.</param>
    /// <param name="now">Current UTC time, used by the expiration filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<(IReadOnlyList<ShortUrl> Items, int TotalCount)> ListAsync(
        ListShortUrlsQuery query, DateTime now, CancellationToken cancellationToken = default);
}
