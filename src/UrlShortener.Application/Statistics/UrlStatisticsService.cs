using UrlShortener.Application.Abstractions;
using UrlShortener.Application.Common;

namespace UrlShortener.Application.Statistics;

/// <summary>Usage statistics of a short link.</summary>
public sealed class UrlStatisticsService(
    IShortUrlRepository shortUrlRepository,
    IUrlVisitRepository visitRepository,
    TimeProvider timeProvider)
{
    /// <summary>Default period when the caller does not give one: the last 30 days, today included.</summary>
    public const int DefaultPeriodDays = 30;

    /// <summary>Upper bound for the period, so a single request cannot ask for an unbounded series.</summary>
    public const int MaxPeriodDays = 366;

    public const int TopReferrersCount = 5;

    /// <param name="shortCode">The link.</param>
    /// <param name="from">First UTC day of the period (inclusive). Defaults to <paramref name="to"/> minus 29 days.</param>
    /// <param name="to">Last UTC day of the period (inclusive). Defaults to today (UTC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<UrlStatisticsResponse> GetAsync(
        string shortCode, DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default)
    {
        ShortCodeGuard.EnsureValid(shortCode);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var periodEnd = to ?? DateOnly.FromDateTime(now);
        var periodStart = from ?? periodEnd.AddDays(-(DefaultPeriodDays - 1));
        ValidatePeriod(periodStart, periodEnd);

        var shortUrl = await shortUrlRepository.GetByShortCodeAsync(shortCode, cancellationToken)
            ?? throw new ShortUrlNotFoundException(shortCode);

        var aggregates = await visitRepository.GetAggregatesAsync(
            shortUrl.Id,
            periodStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            periodEnd.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            TopReferrersCount,
            cancellationToken);

        return new UrlStatisticsResponse(
            shortUrl.ShortCode,
            shortUrl.OriginalUrl,
            shortUrl.CreatedAt,
            shortUrl.ExpiresAt,
            shortUrl.IsActive,
            shortUrl.IsExpired(now),
            aggregates.TotalVisits,
            aggregates.LastVisitAt,
            periodStart,
            periodEnd,
            aggregates.VisitsByDay.Select(d => new DailyVisitsResponse(d.Date, d.Visits)).ToList(),
            aggregates.TopReferrers.Select(r => new ReferrerVisitsResponse(r.Referrer, r.Visits)).ToList());
    }

    private static void ValidatePeriod(DateOnly from, DateOnly to)
    {
        string? error = null;

        if (from > to)
        {
            error = "'from' must be on or before 'to'.";
        }
        else if (to.DayNumber - from.DayNumber + 1 > MaxPeriodDays)
        {
            error = $"The period cannot be longer than {MaxPeriodDays} days.";
        }

        if (error is not null)
        {
            throw new RequestValidationException(new Dictionary<string, string[]> { ["from"] = [error] });
        }
    }
}
