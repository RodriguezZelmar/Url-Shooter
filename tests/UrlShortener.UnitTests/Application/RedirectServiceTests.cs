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
    private readonly InMemoryUrlVisitRepository _visits = new();
    private readonly FakeTimeProvider _time = new(CreatedAt);

    [Fact]
    public async Task ResolveAsync_ReturnsOriginalUrlOfActiveLink()
    {
        _repository.Seed(ShortUrl.Create("https://example.com/page", "abc123X", CreatedAt, null));

        var url = await ResolveAsync("abc123X");

        Assert.Equal("https://example.com/page", url);
    }

    [Fact]
    public async Task ResolveAsync_IsCaseSensitive()
    {
        _repository.Seed(ShortUrl.Create("https://example.com/page", "abc123X", CreatedAt, null));

        await Assert.ThrowsAsync<ShortUrlNotFoundException>(() => ResolveAsync("ABC123x"));
    }

    [Fact]
    public async Task ResolveAsync_ThrowsNotFoundForUnknownCode()
    {
        await Assert.ThrowsAsync<ShortUrlNotFoundException>(() => ResolveAsync("missing1"));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("abc-123")]
    public async Task ResolveAsync_ThrowsNotFoundForMalformedCode(string code)
    {
        await Assert.ThrowsAsync<ShortUrlNotFoundException>(() => ResolveAsync(code));
    }

    [Fact]
    public async Task ResolveAsync_ThrowsNotFoundForInactiveLink()
    {
        var shortUrl = ShortUrl.Create("https://example.com/page", "abc123X", CreatedAt, null);
        shortUrl.Deactivate();
        _repository.Seed(shortUrl);

        await Assert.ThrowsAsync<ShortUrlNotFoundException>(() => ResolveAsync("abc123X"));
        Assert.Empty(_visits.Visits);
    }

    [Fact]
    public async Task ResolveAsync_RedirectsLinkWithoutExpirationForever()
    {
        _repository.Seed(ShortUrl.Create("https://example.com/page", "abc123X", CreatedAt, expiresAt: null));
        _time.Advance(TimeSpan.FromDays(3650));

        Assert.Equal("https://example.com/page", await ResolveAsync("abc123X"));
    }

    [Fact]
    public async Task ResolveAsync_RedirectsLinkThatHasNotExpiredYet()
    {
        _repository.Seed(ShortUrl.Create("https://example.com/page", "abc123X", CreatedAt, CreatedAt.AddHours(1)));
        _time.Advance(TimeSpan.FromMinutes(59));

        Assert.Equal("https://example.com/page", await ResolveAsync("abc123X"));
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

        var exception = await Assert.ThrowsAsync<ShortUrlExpiredException>(() => ResolveAsync("abc123X"));

        Assert.Equal(expiresAt, exception.ExpiredAt);
    }

    [Fact]
    public async Task ResolveAsync_KeepsExpiredLinkStoredAndDoesNotCountTheVisit()
    {
        _repository.Seed(ShortUrl.Create("https://example.com/page", "abc123X", CreatedAt, CreatedAt.AddHours(1)));
        _time.Advance(TimeSpan.FromDays(1));

        await Assert.ThrowsAsync<ShortUrlExpiredException>(() => ResolveAsync("abc123X"));

        Assert.Single(_repository.Items);
        Assert.Empty(_visits.Visits);
    }

    [Fact]
    public async Task ResolveAsync_RecordsVisitWithDateUserAgentAndReferrerOrigin()
    {
        _repository.Seed(ShortUrl.Create("https://example.com/page", "abc123X", CreatedAt, null));
        _time.Advance(TimeSpan.FromMinutes(5));

        await ResolveAsync("abc123X", new VisitorInfo("Mozilla/5.0", "https://news.example.com/a?id=1"));

        var visit = Assert.Single(_visits.Visits);
        Assert.Equal(CreatedAt.AddMinutes(5), visit.VisitedAt);
        Assert.Equal("Mozilla/5.0", visit.UserAgent);
        Assert.Equal("https://news.example.com", visit.Referrer);
    }

    [Fact]
    public async Task ResolveAsync_RecordsOneVisitPerRedirect()
    {
        _repository.Seed(ShortUrl.Create("https://example.com/page", "abc123X", CreatedAt, null));

        for (var i = 0; i < 3; i++)
        {
            await ResolveAsync("abc123X");
        }

        Assert.Equal(3, _visits.Visits.Count);
    }

    [Fact]
    public async Task ResolveAsync_StillRedirectsWhenTheVisitCannotBeRecorded()
    {
        _repository.Seed(ShortUrl.Create("https://example.com/page", "abc123X", CreatedAt, null));
        _visits.FailWith = new InvalidOperationException("database unavailable");

        Assert.Equal("https://example.com/page", await ResolveAsync("abc123X"));
    }

    private Task<string> ResolveAsync(string shortCode, VisitorInfo? visitor = null) =>
        new RedirectService(_repository, _visits, _time, NullLogger<RedirectService>.Instance)
            .ResolveAsync(shortCode, visitor ?? VisitorInfo.Empty);
}
