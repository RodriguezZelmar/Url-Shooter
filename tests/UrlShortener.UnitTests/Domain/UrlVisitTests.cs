using UrlShortener.Domain.Visits;

namespace UrlShortener.UnitTests.Domain;

public sealed class UrlVisitTests
{
    private static readonly DateTime Now = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Record_StoresLinkDateAndUserAgent()
    {
        var visit = UrlVisit.Record(42, Now, "Mozilla/5.0", null);

        Assert.Equal(42, visit.ShortUrlId);
        Assert.Equal(Now, visit.VisitedAt);
        Assert.Equal("Mozilla/5.0", visit.UserAgent);
        Assert.Null(visit.Referrer);
    }

    [Theory]
    [InlineData("https://news.example.com/article/123?user=jane@example.com&token=abc", "https://news.example.com")]
    [InlineData("http://blog.example.org:8080/post", "http://blog.example.org:8080")]
    public void Record_KeepsOnlyTheOriginOfTheReferrer(string referrer, string expected)
    {
        var visit = UrlVisit.Record(1, Now, null, referrer);

        Assert.Equal(expected, visit.Referrer);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("android-app://com.example")]
    public void Record_IgnoresMissingOrNonWebReferrers(string? referrer)
    {
        Assert.Null(UrlVisit.Record(1, Now, null, referrer).Referrer);
    }

    [Fact]
    public void Record_TruncatesVeryLongUserAgents()
    {
        var visit = UrlVisit.Record(1, Now, new string('x', 5_000), null);

        Assert.Equal(UrlVisit.MaxUserAgentLength, visit.UserAgent!.Length);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Record_StoresBlankUserAgentAsNull(string userAgent)
    {
        Assert.Null(UrlVisit.Record(1, Now, userAgent, null).UserAgent);
    }
}
