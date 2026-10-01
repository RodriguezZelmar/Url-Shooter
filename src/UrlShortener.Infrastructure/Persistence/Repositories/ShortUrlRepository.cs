using Microsoft.EntityFrameworkCore;
using UrlShortener.Application.Abstractions;
using UrlShortener.Application.ShortUrls;
using UrlShortener.Domain.ShortUrls;

namespace UrlShortener.Infrastructure.Persistence.Repositories;

internal sealed class ShortUrlRepository(UrlShortenerDbContext dbContext) : IShortUrlRepository
{
    public Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default) =>
        dbContext.ShortUrls.SingleOrDefaultAsync(x => x.ShortCode == shortCode, cancellationToken);

    public Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken cancellationToken = default) =>
        dbContext.ShortUrls.AnyAsync(x => x.ShortCode == shortCode, cancellationToken);

    public async Task AddAsync(ShortUrl shortUrl, CancellationToken cancellationToken = default)
    {
        var entry = dbContext.ShortUrls.Add(shortUrl);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (UniqueConstraint.IsViolation(exception))
        {
            // Stop tracking the rejected entity; otherwise the next SaveChanges (the caller's retry
            // with a new code) would try to insert it again.
            entry.State = EntityState.Detached;
            throw new DuplicateShortCodeException(shortUrl.ShortCode, exception);
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    public async Task<(IReadOnlyList<ShortUrl> Items, int TotalCount)> ListAsync(
        ListShortUrlsQuery query, DateTime now, CancellationToken cancellationToken = default)
    {
        var links = dbContext.ShortUrls.AsNoTracking();

        if (!string.IsNullOrEmpty(query.ShortCode))
        {
            // A prefix match can use the unique index on ShortCode.
            links = links.Where(x => x.ShortCode.StartsWith(query.ShortCode));
        }

        if (query.Status is { } status)
        {
            var active = status == ShortUrlStatus.Active;
            links = links.Where(x => x.IsActive == active);
        }

        if (query.Expired is { } expired)
        {
            links = expired
                ? links.Where(x => x.ExpiresAt != null && x.ExpiresAt <= now)
                : links.Where(x => x.ExpiresAt == null || x.ExpiresAt > now);
        }

        if (query.CreatedBy is { } createdBy)
        {
            links = links.Where(x => x.CreatedBy == createdBy);
        }

        var totalCount = await links.CountAsync(cancellationToken);

        // Id breaks ties between links created at the same instant, so pages are stable.
        var items = await links
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
