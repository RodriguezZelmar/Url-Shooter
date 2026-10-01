namespace UrlShortener.Domain.Visits;

/// <summary>
/// One successful redirection through a short link. Used only for aggregated statistics.
/// </summary>
/// <remarks>
/// Privacy by design: no IP address and no full referrer URL are stored. The referrer is reduced to its
/// origin (scheme + host), because full URLs can carry personal data in their path or query string.
/// </remarks>
public sealed class UrlVisit
{
    public const int MaxUserAgentLength = 512;
    public const int MaxReferrerLength = 256;

    // Required by EF Core.
    private UrlVisit()
    {
    }

    private UrlVisit(long shortUrlId, DateTime visitedAt, string? userAgent, string? referrer)
    {
        ShortUrlId = shortUrlId;
        VisitedAt = visitedAt;
        UserAgent = userAgent;
        Referrer = referrer;
    }

    public long Id { get; private set; }

    public long ShortUrlId { get; private set; }

    public DateTime VisitedAt { get; private set; }

    public string? UserAgent { get; private set; }

    /// <summary>Origin of the referring page (for example "https://news.example.com"), if any.</summary>
    public string? Referrer { get; private set; }

    public static UrlVisit Record(long shortUrlId, DateTime visitedAt, string? userAgent, string? referrer) =>
        new(shortUrlId, visitedAt, Truncate(userAgent, MaxUserAgentLength), ToOrigin(referrer));

    private static string? Truncate(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Length <= maxLength ? value : value[..maxLength];

    private static string? ToOrigin(string? referrer)
    {
        if (!Uri.TryCreate(referrer, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return null;
        }

        return Truncate(uri.GetLeftPart(UriPartial.Authority), MaxReferrerLength);
    }
}
