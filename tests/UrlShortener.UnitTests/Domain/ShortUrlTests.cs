using UrlShortener.Domain.Common;
using UrlShortener.Domain.ShortUrls;

namespace UrlShortener.UnitTests.Domain;

public sealed class ShortUrlTests
{
    private static readonly DateTime Now = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_SetsAllValuesAndStartsActive()
    {
        var shortUrl = ShortUrl.Create("https://example.com", "abc123X", Now, Now.AddDays(1), "marketing");

        Assert.Equal("https://example.com", shortUrl.OriginalUrl);
        Assert.Equal("abc123X", shortUrl.ShortCode);
        Assert.Equal(Now, shortUrl.CreatedAt);
        Assert.Equal(Now.AddDays(1), shortUrl.ExpiresAt);
        Assert.Equal("marketing", shortUrl.CreatedBy);
        Assert.True(shortUrl.IsActive);
    }

    [Theory]
    [InlineData("javascript:alert(1)", "abc123X")]
    [InlineData("https://example.com", "ab")]
    public void Create_RejectsInvalidValuesEvenIfTheCallerSkippedValidation(string url, string code)
    {
        Assert.Throws<DomainValidationException>(() => ShortUrl.Create(url, code, Now, null));
    }

    [Fact]
    public void Create_RejectsExpirationInThePast()
    {
        Assert.Throws<DomainValidationException>(() => ShortUrl.Create("https://example.com", "abc123X", Now, Now.AddMinutes(-1)));
    }

    [Fact]
    public void IsExpired_IsFalseWhenThereIsNoExpiration()
    {
        var shortUrl = ShortUrl.Create("https://example.com", "abc123X", Now, expiresAt: null);

        Assert.False(shortUrl.IsExpired(Now.AddYears(100)));
    }

    [Fact]
    public void IsExpired_IsFalseBeforeTheExpirationDate()
    {
        var shortUrl = ShortUrl.Create("https://example.com", "abc123X", Now, Now.AddHours(1));

        Assert.False(shortUrl.IsExpired(Now.AddMinutes(59)));
    }

    [Fact]
    public void IsExpired_IsTrueFromTheExactExpirationInstant()
    {
        var shortUrl = ShortUrl.Create("https://example.com", "abc123X", Now, Now.AddHours(1));

        Assert.True(shortUrl.IsExpired(Now.AddHours(1)));
        Assert.True(shortUrl.IsExpired(Now.AddDays(1)));
    }

    [Fact]
    public void DeactivateAndActivate_ToggleIsActive()
    {
        var shortUrl = ShortUrl.Create("https://example.com", "abc123X", Now, null);

        shortUrl.Deactivate();
        Assert.False(shortUrl.IsActive);

        shortUrl.Activate();
        Assert.True(shortUrl.IsActive);
    }
}
