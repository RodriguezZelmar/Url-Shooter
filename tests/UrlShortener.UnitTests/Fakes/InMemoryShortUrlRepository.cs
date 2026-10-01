using UrlShortener.Application.Abstractions;
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

    public Task AddAsync(ShortUrl shortUrl, CancellationToken cancellationToken = default)
    {
        if (CodesTakenConcurrently.Contains(shortUrl.ShortCode) || !_items.TryAdd(shortUrl.ShortCode, shortUrl))
        {
            throw new DuplicateShortCodeException(shortUrl.ShortCode);
        }

        return Task.CompletedTask;
    }
}
