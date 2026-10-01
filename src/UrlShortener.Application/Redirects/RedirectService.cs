using Microsoft.Extensions.Logging;
using UrlShortener.Application.Abstractions;
using UrlShortener.Application.Common;
using UrlShortener.Domain.ShortUrls;

namespace UrlShortener.Application.Redirects;

/// <summary>Resolves a short code to the URL a visitor must be sent to.</summary>
public sealed class RedirectService(
    IShortUrlRepository repository,
    TimeProvider timeProvider,
    ILogger<RedirectService> logger)
{
    /// <returns>The original URL to redirect to.</returns>
    /// <exception cref="ShortUrlNotFoundException">The code does not exist, has an invalid format or the link is inactive.</exception>
    /// <exception cref="ShortUrlExpiredException">The link has expired.</exception>
    public async Task<string> ResolveAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        if (ShortUrlRules.ValidateShortCode(shortCode) is not null)
        {
            throw new ShortUrlNotFoundException(shortCode);
        }

        var shortUrl = await repository.GetByShortCodeAsync(shortCode, cancellationToken)
            ?? throw new ShortUrlNotFoundException(shortCode);

        // A deactivated link answers exactly like a missing one: visitors learn nothing about it.
        if (!shortUrl.IsActive)
        {
            logger.LogInformation("Redirect refused: short URL {ShortCode} is inactive", shortCode);
            throw new ShortUrlNotFoundException(shortCode);
        }

        // Expired links are kept (with their statistics); they simply stop redirecting.
        if (shortUrl.IsExpired(timeProvider.GetUtcNow().UtcDateTime))
        {
            logger.LogInformation("Redirect refused: short URL {ShortCode} expired at {ExpiresAt:O}", shortCode, shortUrl.ExpiresAt);
            throw new ShortUrlExpiredException(shortCode, shortUrl.ExpiresAt!.Value);
        }

        return shortUrl.OriginalUrl;
    }
}
