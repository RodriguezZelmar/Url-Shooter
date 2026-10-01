using UrlShortener.Application.Abstractions;
using UrlShortener.Domain.Visits;

namespace UrlShortener.UnitTests.Fakes;

internal sealed class InMemoryUrlVisitRepository : IUrlVisitRepository
{
    private readonly List<UrlVisit> _visits = [];

    /// <summary>When set, <see cref="AddAsync"/> throws it, simulating a database failure.</summary>
    public Exception? FailWith { get; set; }

    public IReadOnlyList<UrlVisit> Visits => _visits;

    public Task AddAsync(UrlVisit visit, CancellationToken cancellationToken = default)
    {
        if (FailWith is not null)
        {
            throw FailWith;
        }

        _visits.Add(visit);
        return Task.CompletedTask;
    }
}
