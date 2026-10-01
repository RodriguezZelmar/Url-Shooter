using Microsoft.Extensions.Logging;
using UrlShortener.Application.Abstractions;
using UrlShortener.Application.Common;
using UrlShortener.Domain.ShortUrls;
using UrlShortener.Domain.Visits;

namespace UrlShortener.Application.Redirects;

/// <summary>Resolves a short code to the URL a visitor must be sent to, and records the visit.</summary>
public sealed class RedirectService(
    IShortUrlRepository shortUrlRepository,
    IUrlVisitRepository visitRepository,
    TimeProvider timeProvider,
    ILogger<RedirectService> logger)
{
    /// <returns>The original URL to redirect to.</returns>
    /// <exception cref="ShortUrlNotFoundException">The code does not exist, has an invalid format or the link is inactive.</exception>
    /// <exception cref="ShortUrlExpiredException">The link has expired.</exception>
    public async Task<string> ResolveAsync(string shortCode, VisitorInfo visitor, CancellationToken cancellationToken = default)
    {
        if (ShortUrlRules.ValidateShortCode(shortCode) is not null)
        {
            throw new ShortUrlNotFoundException(shortCode);
        }

        var shortUrl = await shortUrlRepository.GetByShortCodeAsync(shortCode, cancellationToken)
            ?? throw new ShortUrlNotFoundException(shortCode);

        // A deactivated link answers exactly like a missing one: visitors learn nothing about it.
        if (!shortUrl.IsActive)
        {
            logger.LogInformation("Redirect refused: short URL {ShortCode} is inactive", shortCode);
            throw new ShortUrlNotFoundException(shortCode);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Expired links are kept (with their statistics); they simply stop redirecting.
        if (shortUrl.IsExpired(now))
        {
            logger.LogInformation("Redirect refused: short URL {ShortCode} expired at {ExpiresAt:O}", shortCode, shortUrl.ExpiresAt);
            throw new ShortUrlExpiredException(shortCode, shortUrl.ExpiresAt!.Value);
        }

        // Only successful redirects count as visits.
        await RecordVisitAsync(shortUrl, visitor, now, cancellationToken);

        return shortUrl.OriginalUrl;
    }

    private async Task RecordVisitAsync(ShortUrl shortUrl, VisitorInfo visitor, DateTime now, CancellationToken cancellationToken)
    {
        try
        {
            await visitRepository.AddAsync(UrlVisit.Record(shortUrl.Id, now, visitor.UserAgent, visitor.Referrer), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Statistics are secondary: a failure to record a visit must never break the link itself.
            logger.LogError(exception, "Could not record visit for short URL {ShortCode}", shortUrl.ShortCode);
        }
    }
}
