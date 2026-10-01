using UrlShortener.Application.Abstractions;
using UrlShortener.Application.ShortUrls;
using UrlShortener.Domain.ShortUrls;

namespace UrlShortener.UnitTests.Fakes;

/// <summary>Repository double that behaves like the real one, including the unique short code constraint.</summary>
internal sealed class InMemoryShortUrlRepository : IShortUrlRepository
{
    private readonly Dictionary<string, ShortUrl> _items = new(StringComparer.Ordinal);

    /// <summary>
    /// Codes that the "existence check" reports as free but the "database" rejects on insert,
    /// simulating another request inserting the same code concurrently.
    /// </summary>
    public HashSet<string> CodesTakenConcurrently { get; } = [];

    public IReadOnlyCollection<ShortUrl> Items => _items.Values;

    public void Seed(ShortUrl shortUrl) => _items.Add(shortUrl.ShortCode, shortUrl);

    public Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.GetValueOrDefault(shortCode));

    public Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.ContainsKey(shortCode));

    public int SaveCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }

    public Task<(IReadOnlyList<ShortUrl> Items, int TotalCount)> ListAsync(
        ListShortUrlsQuery query, DateTime now, CancellationToken cancellationToken = default)
    {
        var items = _items.Values.OrderByDescending(x => x.CreatedAt).ToList();
        return Task.FromResult<(IReadOnlyList<ShortUrl>, int)>((items.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList(), items.Count));
    }

    public Task AddAsync(ShortUrl shortUrl, CancellationToken cancellationToken = default)
    {
        if (CodesTakenConcurrently.Contains(shortUrl.ShortCode) || !_items.TryAdd(shortUrl.ShortCode, shortUrl))
        {
            throw new DuplicateShortCodeException(shortUrl.ShortCode);
        }

        return Task.CompletedTask;
    }
}
