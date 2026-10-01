namespace UrlShortener.Application.Redirects;

/// <summary>
/// The only request data the service keeps about a visit. Deliberately excludes the IP address.
/// </summary>
/// <param name="UserAgent">Raw <c>User-Agent</c> header, if present.</param>
/// <param name="Referrer">Raw <c>Referer</c> header, if present. Only its origin is stored.</param>
public sealed record VisitorInfo(string? UserAgent, string? Referrer)
{
    public static VisitorInfo Empty { get; } = new(null, null);
}
