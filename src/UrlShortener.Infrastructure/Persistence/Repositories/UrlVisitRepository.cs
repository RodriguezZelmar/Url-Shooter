using Microsoft.EntityFrameworkCore;
using UrlShortener.Application.Abstractions;
using UrlShortener.Domain.Visits;

namespace UrlShortener.Infrastructure.Persistence.Repositories;

internal sealed class UrlVisitRepository(UrlShortenerDbContext dbContext) : IUrlVisitRepository
{
    public async Task AddAsync(UrlVisit visit, CancellationToken cancellationToken = default)
    {
        dbContext.UrlVisits.Add(visit);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // Every query filters by ShortUrlId first, so all of them use the (ShortUrlId, VisitedAt) index,
    // and every aggregation (COUNT, MAX, GROUP BY) runs in the database: only the results travel.
    public async Task<VisitAggregates> GetAggregatesAsync(
        long shortUrlId, DateTime fromUtc, DateTime toUtcExclusive, int topReferrers, CancellationToken cancellationToken = default)
    {
        var visits = dbContext.UrlVisits.AsNoTracking().Where(v => v.ShortUrlId == shortUrlId);

        // SELECT COUNT(*), MAX(VisitedAt) ... in a single round trip.
        var totals = await visits
            .GroupBy(_ => 1)
            .Select(g => new { Count = g.Count(), Last = g.Max(v => v.VisitedAt) })
            .SingleOrDefaultAsync(cancellationToken);

        var inPeriod = visits.Where(v => v.VisitedAt >= fromUtc && v.VisitedAt < toUtcExclusive);

        // GROUP BY the UTC calendar day.
        var byDay = await inPeriod
            .GroupBy(v => v.VisitedAt.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .OrderBy(x => x.Day)
            .ToListAsync(cancellationToken);

        var referrers = await inPeriod
            .Where(v => v.Referrer != null)
            .GroupBy(v => v.Referrer!)
            .Select(g => new { Referrer = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Referrer)
            .Take(topReferrers)
            .ToListAsync(cancellationToken);

        return new VisitAggregates(
            totals?.Count ?? 0,
            totals?.Last,
            byDay.Select(x => new DailyVisitCount(DateOnly.FromDateTime(x.Day), x.Count)).ToList(),
            referrers.Select(x => new ReferrerVisitCount(x.Referrer, x.Count)).ToList());
    }
}
