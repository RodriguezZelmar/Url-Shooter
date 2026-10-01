namespace UrlShortener.Application.Options;

/// <summary>
/// Service settings, bound from the <c>UrlShortener</c> configuration section and checked by
/// <see cref="UrlShortenerOptionsValidator"/> when the application starts.
/// </summary>
public sealed class UrlShortenerOptions
{
    public const string SectionName = "UrlShortener";

    /// <summary>
    /// Public base address used to build short URLs (for example "https://sho.rt").
    /// It is the only place where the service's own address is configured.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Length of generated short codes. With Base62, 7 characters give 62^7 ≈ 3.5 trillion codes.</summary>
    public int ShortCodeLength { get; set; } = 7;

    /// <summary>How many codes are tried before giving up when generated codes collide with existing ones.</summary>
    public int MaxGenerationAttempts { get; set; } = 5;

    /// <summary>
    /// Lifetime applied to links created without an explicit expiration date. <c>null</c> (the default) means
    /// such links never expire.
    /// </summary>
    public TimeSpan? DefaultLifetime { get; set; }

    /// <summary>Builds the public short URL for a code, e.g. "https://sho.rt/a8F2k".</summary>
    public string BuildShortUrl(string shortCode) => $"{BaseUrl.TrimEnd('/')}/{shortCode}";
}
