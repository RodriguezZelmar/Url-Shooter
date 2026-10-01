using Microsoft.Extensions.Logging.Abstractions;
using UrlShortener.Application.Common;
using UrlShortener.Application.Redirects;
using UrlShortener.Domain.ShortUrls;
using UrlShortener.UnitTests.Fakes;

namespace UrlShortener.UnitTests.Application;

public sealed class RedirectServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly InMemoryShortUrlRepository _repository = new();

    [Fact]
    public async Task ResolveAsync_ReturnsOriginalUrlOfActiveLink()
    {
        _repository.Seed(ShortUrl.Create("https://example.com/page", "abc123X", Now, null));

        var url = await CreateService().ResolveAsync("abc123X");

        Assert.Equal("https://example.com/page", url);
    }

    [Fact]
    public async Task ResolveAsync_IsCaseSensitive()
    {
        _repository.Seed(ShortUrl.Create("https://example.com/page", "abc123X", Now, null));

        await Assert.ThrowsAsync<ShortUrlNotFoundException>(() => CreateService().ResolveAsync("ABC123x"));
    }

    [Fact]
    public async Task ResolveAsync_ThrowsNotFoundForUnknownCode()
    {
        await Assert.ThrowsAsync<ShortUrlNotFoundException>(() => CreateService().ResolveAsync("missing1"));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("abc-123")]
    public async Task ResolveAsync_ThrowsNotFoundForMalformedCode(string code)
    {
        await Assert.ThrowsAsync<ShortUrlNotFoundException>(() => CreateService().ResolveAsync(code));
    }

    [Fact]
    public async Task ResolveAsync_ThrowsNotFoundForInactiveLink()
    {
        var shortUrl = ShortUrl.Create("https://example.com/page", "abc123X", Now, null);
        shortUrl.Deactivate();
        _repository.Seed(shortUrl);

        await Assert.ThrowsAsync<ShortUrlNotFoundException>(() => CreateService().ResolveAsync("abc123X"));
    }

    private RedirectService CreateService() => new(_repository, NullLogger<RedirectService>.Instance);
}
