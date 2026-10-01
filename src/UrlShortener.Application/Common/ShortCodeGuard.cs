using UrlShortener.Domain.ShortUrls;

namespace UrlShortener.Application.Common;

internal static class ShortCodeGuard
{
    /// <summary>Rejects a malformed short code received as input with a 400-style validation error.</summary>
    public static void EnsureValid(string shortCode)
    {
        if (ShortUrlRules.ValidateShortCode(shortCode) is { } error)
        {
            throw new RequestValidationException(new Dictionary<string, string[]> { ["shortCode"] = [error] });
        }
    }
}
