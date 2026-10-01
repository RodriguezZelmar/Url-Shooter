namespace UrlShortener.Domain.ShortUrls;

/// <summary>
/// Rules that decide whether a URL, short code or expiration date is acceptable.
/// Each method returns an error message, or <c>null</c> when the value is valid, so the same rules
/// can report friendly validation errors (application layer) and guard the entity (domain layer).
/// </summary>
public static class ShortUrlRules
{
    /// <summary>Practical upper bound used by browsers and most servers for a URL.</summary>
    public const int MaxUrlLength = 2048;

    public const int MinShortCodeLength = 4;
    public const int MaxShortCodeLength = 32;

    public const int MaxCreatedByLength = 100;

    /// <summary>Alphabet used for generated codes: Base62, safe in any URL path without encoding.</summary>
    public const string ShortCodeAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string? ValidateOriginalUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return "The URL is required.";
        }

        if (url.Length > MaxUrlLength)
        {
            return $"The URL must be at most {MaxUrlLength} characters long.";
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return "The URL is not a valid absolute URL.";
        }

        // Allow-list instead of a deny-list: rejects "javascript:", "data:", "file:", "ftp:" and anything else.
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return "Only http and https URLs are allowed.";
        }

        if (string.IsNullOrEmpty(uri.Host))
        {
            return "The URL must include a host.";
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            // "https://trusted.com@evil.com" style URLs are a common phishing trick.
            return "URLs with embedded credentials are not allowed.";
        }

        return null;
    }

    public static string? ValidateShortCode(string? shortCode)
    {
        if (string.IsNullOrEmpty(shortCode))
        {
            return "The short code is required.";
        }

        if (shortCode.Length is < MinShortCodeLength or > MaxShortCodeLength)
        {
            return $"The short code must be between {MinShortCodeLength} and {MaxShortCodeLength} characters long.";
        }

        foreach (var c in shortCode)
        {
            if (!char.IsAsciiLetterOrDigit(c))
            {
                return "The short code may only contain letters (A-Z, a-z) and digits (0-9).";
            }
        }

        return null;
    }

    /// <summary>The expiration date must be in the future at the moment the link is created.</summary>
    public static string? ValidateExpiration(DateTime? expiresAt, DateTime now)
    {
        if (expiresAt is { } value && value <= now)
        {
            return "The expiration date must be in the future.";
        }

        return null;
    }

    public static string? ValidateCreatedBy(string? createdBy)
    {
        if (createdBy is not null && (string.IsNullOrWhiteSpace(createdBy) || createdBy.Length > MaxCreatedByLength))
        {
            return $"CreatedBy must be a non-empty value of at most {MaxCreatedByLength} characters.";
        }

        return null;
    }
}
