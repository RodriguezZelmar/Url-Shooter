using Microsoft.EntityFrameworkCore;
using UrlShortener.Application.Abstractions;
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
}
