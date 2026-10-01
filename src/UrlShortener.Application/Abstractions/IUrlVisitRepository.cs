using UrlShortener.Domain.Visits;

namespace UrlShortener.Application.Abstractions;

/// <summary>Persistence of <see cref="UrlVisit"/> records.</summary>
public interface IUrlVisitRepository
{
    /// <summary>Inserts the visit and saves it immediately.</summary>
    Task AddAsync(UrlVisit visit, CancellationToken cancellationToken = default);
}
