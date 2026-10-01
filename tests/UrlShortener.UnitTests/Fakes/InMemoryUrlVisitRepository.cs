using UrlShortener.Application.Abstractions;
using UrlShortener.Domain.Visits;

namespace UrlShortener.UnitTests.Fakes;

internal sealed class InMemoryUrlVisitRepository : IUrlVisitRepository
{
    private readonly List<UrlVisit> _visits = [];

    /// <summary>When set, <see cref="AddAsync"/> throws it, simulating a database failure.</summary>
    public Exception? FailWith { get; set; }

    public IReadOnlyList<UrlVisit> Visits => _visits;

    /// <summary>
    /// Canned result of <see cref="GetAggregatesAsync"/>. The SQL aggregation itself is covered by the
    /// integration tests against a real database; unit tests only check how the service uses it.
    /// </summary>
    public VisitAggregates Aggregates { get; set; } = new(0, null, [], []);

    public (long ShortUrlId, DateTime From, DateTime ToExclusive, int TopReferrers)? LastAggregatesQuery { get; private set; }

    public Task AddAsync(UrlVisit visit, CancellationToken cancellationToken = default)
    {
        if (FailWith is not null)
        {
            throw FailWith;
        }

        _visits.Add(visit);
        return Task.CompletedTask;
    }

    public Task<VisitAggregates> GetAggregatesAsync(
        long shortUrlId, DateTime fromUtc, DateTime toUtcExclusive, int topReferrers, CancellationToken cancellationToken = default)
    {
        LastAggregatesQuery = (shortUrlId, fromUtc, toUtcExclusive, topReferrers);
        return Task.FromResult(Aggregates);
    }
}
