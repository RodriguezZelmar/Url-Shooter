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
}
