namespace UrlShortener.Application.Statistics;

/// <summary>Usage statistics of a short link.</summary>
/// <param name="ShortCode">Public identifier of the link.</param>
/// <param name="OriginalUrl">Destination of the redirect.</param>
/// <param name="CreatedAt">Creation date (UTC).</param>
/// <param name="ExpiresAt">Expiration date (UTC), or null if the link never expires.</param>
/// <param name="IsActive">False when an administrator has disabled the link.</param>
/// <param name="IsExpired">True when the expiration date has passed.</param>
/// <param name="TotalVisits">Successful redirects since the link was created.</param>
/// <param name="LastVisitAt">Date of the most recent visit (UTC), or null if never visited.</param>
/// <param name="From">First day (UTC) covered by <paramref name="VisitsByDay"/> and <paramref name="TopReferrers"/>.</param>
/// <param name="To">Last day (UTC, inclusive) covered by <paramref name="VisitsByDay"/> and <paramref name="TopReferrers"/>.</param>
/// <param name="VisitsByDay">Visits per UTC day within the period, ascending. Days without visits are omitted.</param>
/// <param name="TopReferrers">Up to five referrer origins with the most visits within the period.</param>
public sealed record UrlStatisticsResponse(
    string ShortCode,
    string OriginalUrl,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    bool IsActive,
    bool IsExpired,
    int TotalVisits,
    DateTime? LastVisitAt,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<DailyVisitsResponse> VisitsByDay,
    IReadOnlyList<ReferrerVisitsResponse> TopReferrers);

/// <param name="Date">UTC calendar day.</param>
/// <param name="Visits">Number of visits on that day.</param>
public sealed record DailyVisitsResponse(DateOnly Date, int Visits);

/// <param name="Referrer">Origin of the referring page, e.g. "https://news.example.com".</param>
/// <param name="Visits">Number of visits coming from that origin.</param>
public sealed record ReferrerVisitsResponse(string Referrer, int Visits);
