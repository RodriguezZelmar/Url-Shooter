using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using UrlShortener.Application.Common;
using UrlShortener.Application.Redirects;
using UrlShortener.Domain.ShortUrls;
using UrlShortener.UnitTests.Fakes;

namespace UrlShortener.UnitTests.Application;

public sealed class RedirectServiceTests
{
    private static readonly DateTime CreatedAt = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly InMemoryShortUrlRepository _repository = new();
    private readonly FakeTimeProvider _time = new(CreatedAt);

    [Fact]
    public async Task ResolveAsync_ReturnsOriginalUrlOfActiveLink()
    {
        _repository.Seed(ShortUrl.Create("https://example.com/page", "abc123X", CreatedAt, null));

        var url = await CreateService().ResolveAsync("abc123X");

        Assert.Equal("https://example.com/page", url);
    }

    [Fact]
    public async Task ResolveAsync_IsCaseSensitive()
    {
        _repository.Seed(ShortUrl.Create("https://example.com/page", "abc123X", CreatedAt, null));

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
        var shortUrl = ShortUrl.Create("https://example.com/page", "abc123X", CreatedAt, null);
        shortUrl.Deactivate();
        _repository.Seed(shortUrl);

        await Assert.ThrowsAsync<ShortUrlNotFoundException>(() => CreateService().ResolveAsync("abc123X"));
    }

    [Fact]
    public async Task ResolveAsync_RedirectsLinkWithoutExpirationForever()
    {
        _repository.Seed(ShortUrl.Create("https://example.com/page", "abc123X", CreatedAt, expiresAt: null));
        _time.Advance(TimeSpan.FromDays(3650));

        Assert.Equal("https://example.com/page", await CreateService().ResolveAsync("abc123X"));
    }

    [Fact]
    public async Task ResolveAsync_RedirectsLinkThatHasNotExpiredYet()
    {
        _repository.Seed(ShortUrl.Create("https://example.com/page", "abc123X", CreatedAt, CreatedAt.AddHours(1)));
        _time.Advance(TimeSpan.FromMinutes(59));

        Assert.Equal("https://example.com/page", await CreateService().ResolveAsync("abc123X"));
    }

    [Theory]
    [InlineData(60)]
    [InlineData(61)]
    [InlineData(60 * 24 * 365)]
    public async Task ResolveAsync_ThrowsExpiredFromTheExpirationInstantOnwards(int minutesLater)
    {
        var expiresAt = CreatedAt.AddHours(1);
        _repository.Seed(ShortUrl.Create("https://example.com/page", "abc123X", CreatedAt, expiresAt));
        _time.Advance(TimeSpan.FromMinutes(minutesLater));

        var exception = await Assert.ThrowsAsync<ShortUrlExpiredException>(() => CreateService().ResolveAsync("abc123X"));

        Assert.Equal(expiresAt, exception.ExpiredAt);
    }

    [Fact]
    public async Task ResolveAsync_KeepsExpiredLinkStored()
    {
        _repository.Seed(ShortUrl.Create("https://example.com/page", "abc123X", CreatedAt, CreatedAt.AddHours(1)));
        _time.Advance(TimeSpan.FromDays(1));

        await Assert.ThrowsAsync<ShortUrlExpiredException>(() => CreateService().ResolveAsync("abc123X"));

        Assert.Single(_repository.Items);
    }

    private RedirectService CreateService() => new(_repository, _time, NullLogger<RedirectService>.Instance);
}
