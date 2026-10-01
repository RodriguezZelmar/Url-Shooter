using Microsoft.Extensions.Time.Testing;
using UrlShortener.Application.Abstractions;
using UrlShortener.Application.Common;
using UrlShortener.Application.Statistics;
using UrlShortener.Domain.ShortUrls;
using UrlShortener.UnitTests.Fakes;

namespace UrlShortener.UnitTests.Application;

public sealed class UrlStatisticsServiceTests
{
    private static readonly DateTime CreatedAt = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 15, 30, 0, TimeSpan.Zero);

    private readonly InMemoryShortUrlRepository _shortUrls = new();
    private readonly InMemoryUrlVisitRepository _visits = new();
    private readonly FakeTimeProvider _time = new(Now);
    private readonly UrlStatisticsService _service;

    public UrlStatisticsServiceTests()
    {
        _service = new UrlStatisticsService(_shortUrls, _visits, _time);
        _shortUrls.Seed(ShortUrl.Create("https://example.com", "abc123X", CreatedAt, CreatedAt.AddDays(60)));
    }

    [Fact]
    public async Task GetAsync_DefaultsToTheLast30DaysIncludingToday()
    {
        var stats = await _service.GetAsync("abc123X");

        Assert.Equal(new DateOnly(2026, 9, 2), stats.From);
        Assert.Equal(new DateOnly(2026, 10, 1), stats.To);

        var query = _visits.LastAggregatesQuery!.Value;
        Assert.Equal(new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc), query.From);
        Assert.Equal(new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc), query.ToExclusive);
        Assert.Equal(UrlStatisticsService.TopReferrersCount, query.TopReferrers);
    }

    [Fact]
    public async Task GetAsync_UsesTheRequestedPeriodWithInclusiveEndDay()
    {
        var stats = await _service.GetAsync("abc123X", new DateOnly(2026, 9, 10), new DateOnly(2026, 9, 12));

        Assert.Equal(new DateOnly(2026, 9, 10), stats.From);
        Assert.Equal(new DateOnly(2026, 9, 12), stats.To);
        Assert.Equal(new DateTime(2026, 9, 13, 0, 0, 0, DateTimeKind.Utc), _visits.LastAggregatesQuery!.Value.ToExclusive);
    }

    [Fact]
    public async Task GetAsync_MapsLinkDataAndAggregates()
    {
        _visits.Aggregates = new VisitAggregates(
            42,
            Now.UtcDateTime.AddHours(-1),
            [new DailyVisitCount(new DateOnly(2026, 9, 30), 12), new DailyVisitCount(new DateOnly(2026, 10, 1), 30)],
            [new ReferrerVisitCount("https://news.example.com", 20)]);

        var stats = await _service.GetAsync("abc123X");

        Assert.Equal("abc123X", stats.ShortCode);
        Assert.Equal("https://example.com", stats.OriginalUrl);
        Assert.Equal(CreatedAt, stats.CreatedAt);
        Assert.Equal(CreatedAt.AddDays(60), stats.ExpiresAt);
        Assert.True(stats.IsActive);
        Assert.False(stats.IsExpired);
        Assert.Equal(42, stats.TotalVisits);
        Assert.Equal(Now.UtcDateTime.AddHours(-1), stats.LastVisitAt);
        Assert.Equal([new DailyVisitsResponse(new DateOnly(2026, 9, 30), 12), new DailyVisitsResponse(new DateOnly(2026, 10, 1), 30)], stats.VisitsByDay);
        Assert.Equal([new ReferrerVisitsResponse("https://news.example.com", 20)], stats.TopReferrers);
    }

    [Fact]
    public async Task GetAsync_IsAvailableForExpiredLinks()
    {
        _time.Advance(TimeSpan.FromDays(90));

        var stats = await _service.GetAsync("abc123X");

        Assert.True(stats.IsExpired);
    }

    [Fact]
    public async Task GetAsync_RejectsPeriodWhereFromIsAfterTo()
    {
        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => _service.GetAsync("abc123X", new DateOnly(2026, 9, 12), new DateOnly(2026, 9, 10)));

        Assert.Contains("from", exception.Errors.Keys);
    }

    [Fact]
    public async Task GetAsync_AcceptsPeriodOfExactlyTheMaximumLength()
    {
        var to = new DateOnly(2026, 10, 1);

        var stats = await _service.GetAsync("abc123X", to.AddDays(-(UrlStatisticsService.MaxPeriodDays - 1)), to);

        Assert.Equal(to, stats.To);
    }

    [Fact]
    public async Task GetAsync_RejectsPeriodLongerThanTheMaximum()
    {
        var to = new DateOnly(2026, 10, 1);

        await Assert.ThrowsAsync<RequestValidationException>(
            () => _service.GetAsync("abc123X", to.AddDays(-UrlStatisticsService.MaxPeriodDays), to));
    }

    [Fact]
    public async Task GetAsync_ThrowsNotFoundForUnknownCode()
    {
        await Assert.ThrowsAsync<ShortUrlNotFoundException>(() => _service.GetAsync("missing1"));
    }

    [Fact]
    public async Task GetAsync_RejectsMalformedCode()
    {
        var exception = await Assert.ThrowsAsync<RequestValidationException>(() => _service.GetAsync("a-b"));

        Assert.Contains("shortCode", exception.Errors.Keys);
    }
}
