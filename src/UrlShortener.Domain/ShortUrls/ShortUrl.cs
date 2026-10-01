using UrlShortener.Domain.Common;

namespace UrlShortener.Domain.ShortUrls;

/// <summary>
/// A shortened link: the public <see cref="ShortCode"/> that maps to an <see cref="OriginalUrl"/>.
/// </summary>
/// <remarks>
/// <see cref="Id"/> is an internal database key and is never exposed; clients only see the short code,
/// which is generated independently so it reveals nothing about how many links exist.
/// All dates are UTC.
/// </remarks>
public sealed class ShortUrl
{
    // Required by EF Core.
    private ShortUrl()
    {
        OriginalUrl = null!;
        ShortCode = null!;
    }

    private ShortUrl(string originalUrl, string shortCode, DateTime createdAt, DateTime? expiresAt, string? createdBy)
    {
        OriginalUrl = originalUrl;
        ShortCode = shortCode;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        CreatedBy = createdBy;
        IsActive = true;
    }

    public long Id { get; private set; }

    public string OriginalUrl { get; private set; }

    public string ShortCode { get; private set; }

    public DateTime CreatedAt { get; private set; }

    /// <summary>When the link stops redirecting. <c>null</c> means it never expires.</summary>
    public DateTime? ExpiresAt { get; private set; }

    /// <summary>Lets an administrator disable a link without deleting it (or its statistics).</summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Optional free-form label of the system or user that created the link (for example "marketing-service").
    /// It is informational only: the service has no authentication, so it is not an identity.
    /// </summary>
    public string? CreatedBy { get; private set; }

    public static ShortUrl Create(string originalUrl, string shortCode, DateTime createdAt, DateTime? expiresAt, string? createdBy = null)
    {
        Guard(ShortUrlRules.ValidateOriginalUrl(originalUrl));
        Guard(ShortUrlRules.ValidateShortCode(shortCode));
        Guard(ShortUrlRules.ValidateExpiration(expiresAt, createdAt));
        Guard(ShortUrlRules.ValidateCreatedBy(createdBy));

        return new ShortUrl(originalUrl, shortCode, createdAt, expiresAt, createdBy);
    }

    /// <summary>A link is expired from the exact <see cref="ExpiresAt"/> instant onwards.</summary>
    public bool IsExpired(DateTime now) => ExpiresAt is { } expiresAt && expiresAt <= now;

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    private static void Guard(string? error)
    {
        if (error is not null)
        {
            throw new DomainValidationException(error);
        }
    }
}
