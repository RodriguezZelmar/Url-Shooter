using System.ComponentModel.DataAnnotations;
using UrlShortener.Domain.ShortUrls;

namespace UrlShortener.Application.ShortUrls;

// The data annotations give the API automatic 400 responses for malformed input (first validation layer).
// ShortUrlService checks the full business rules again (second layer), so callers that do not go
// through ASP.NET Core model validation are protected as well.

/// <summary>Request to shorten a URL.</summary>
/// <param name="Url">Absolute http or https URL to shorten.</param>
/// <param name="ExpiresAt">Optional moment (with offset or UTC) from which the link stops redirecting. Must be in the future.</param>
/// <param name="CustomCode">Optional custom short code (4-32 letters and digits). When omitted, a random code is generated.</param>
/// <param name="CreatedBy">Optional free-form label of the creating system or user (for example "marketing-service").</param>
public sealed record CreateShortUrlRequest(
    [Required, MaxLength(ShortUrlRules.MaxUrlLength)] string Url,
    DateTimeOffset? ExpiresAt = null,
    [MinLength(ShortUrlRules.MinShortCodeLength), MaxLength(ShortUrlRules.MaxShortCodeLength)] string? CustomCode = null,
    [MaxLength(ShortUrlRules.MaxCreatedByLength)] string? CreatedBy = null);

/// <summary>A short link as returned by the API.</summary>
/// <param name="ShortCode">Public identifier of the link.</param>
/// <param name="ShortUrl">Full short URL, built from the configured public base URL.</param>
/// <param name="OriginalUrl">Destination of the redirect.</param>
/// <param name="CreatedAt">Creation date (UTC).</param>
/// <param name="ExpiresAt">Expiration date (UTC), or null if the link never expires.</param>
/// <param name="IsActive">False when an administrator has disabled the link.</param>
/// <param name="CreatedBy">Optional label supplied when the link was created.</param>
public sealed record ShortUrlResponse(
    string ShortCode,
    string ShortUrl,
    string OriginalUrl,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    bool IsActive,
    string? CreatedBy);
