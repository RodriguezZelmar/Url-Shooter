using UrlShortener.Domain.Visits;

namespace UrlShortener.Application.Abstractions;

/// <summary>Persistence and aggregation of <see cref="UrlVisit"/> records.</summary>
public interface IUrlVisitRepository
{
    /// <summary>Inserts the visit and saves it immediately.</summary>
    Task AddAsync(UrlVisit visit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aggregates the visits of one link in the database (nothing is loaded row by row).
    /// Totals cover all time; daily counts and top referrers cover <c>[fromUtc, toUtcExclusive)</c>.
    /// </summary>
    Task<VisitAggregates> GetAggregatesAsync(
        long shortUrlId, DateTime fromUtc, DateTime toUtcExclusive, int topReferrers, CancellationToken cancellationToken = default);
}

public sealed record VisitAggregates(
    int TotalVisits,
    DateTime? LastVisitAt,
    IReadOnlyList<DailyVisitCount> VisitsByDay,
    IReadOnlyList<ReferrerVisitCount> TopReferrers);

public sealed record DailyVisitCount(DateOnly Date, int Visits);

public sealed record ReferrerVisitCount(string Referrer, int Visits);
