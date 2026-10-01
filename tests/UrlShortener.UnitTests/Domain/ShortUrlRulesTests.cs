using UrlShortener.Domain.ShortUrls;

namespace UrlShortener.UnitTests.Domain;

public sealed class ShortUrlRulesTests
{
    private static readonly DateTime Now = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com/path?query=1#fragment")]
    [InlineData("https://sub.example.co.uk:8443/a/very/long/path")]
    [InlineData("https://例え.jp/ページ")]
    public void ValidateOriginalUrl_AcceptsHttpAndHttpsUrls(string url)
    {
        Assert.Null(ShortUrlRules.ValidateOriginalUrl(url));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateOriginalUrl_RejectsEmptyUrl(string? url)
    {
        Assert.Equal("The URL is required.", ShortUrlRules.ValidateOriginalUrl(url));
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("example.com")]
    [InlineData("/relative/path")]
    [InlineData("https://")]
    public void ValidateOriginalUrl_RejectsInvalidUrls(string url)
    {
        Assert.NotNull(ShortUrlRules.ValidateOriginalUrl(url));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("JavaScript:alert(document.cookie)")]
    [InlineData("data:text/html;base64,PHNjcmlwdD4=")]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com/file")]
    [InlineData("mailto:someone@example.com")]
    public void ValidateOriginalUrl_RejectsDangerousOrUnsupportedSchemes(string url)
    {
        Assert.Equal("Only http and https URLs are allowed.", ShortUrlRules.ValidateOriginalUrl(url));
    }

    [Fact]
    public void ValidateOriginalUrl_RejectsEmbeddedCredentials()
    {
        Assert.Equal("URLs with embedded credentials are not allowed.",
            ShortUrlRules.ValidateOriginalUrl("https://trusted.com@evil.com/login"));
    }

    [Fact]
    public void ValidateOriginalUrl_AcceptsUrlAtMaximumLength()
    {
        var url = UrlOfLength(ShortUrlRules.MaxUrlLength);

        Assert.Null(ShortUrlRules.ValidateOriginalUrl(url));
    }

    [Fact]
    public void ValidateOriginalUrl_RejectsUrlLongerThanMaximum()
    {
        var url = UrlOfLength(ShortUrlRules.MaxUrlLength + 1);

        Assert.Equal($"The URL must be at most {ShortUrlRules.MaxUrlLength} characters long.",
            ShortUrlRules.ValidateOriginalUrl(url));
    }

    [Theory]
    [InlineData("abcd")]
    [InlineData("a8F2k9Q")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ012345")]
    public void ValidateShortCode_AcceptsAlphanumericCodesWithinLimits(string code)
    {
        Assert.Null(ShortUrlRules.ValidateShortCode(code));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456")]
    [InlineData("abc-d")]
    [InlineData("abc_d")]
    [InlineData("abc d")]
    [InlineData("abc/d")]
    [InlineData("ñandú")]
    public void ValidateShortCode_RejectsInvalidCodes(string code)
    {
        Assert.NotNull(ShortUrlRules.ValidateShortCode(code));
    }

    [Fact]
    public void ValidateExpiration_AcceptsNoExpiration()
    {
        Assert.Null(ShortUrlRules.ValidateExpiration(null, Now));
    }

    [Fact]
    public void ValidateExpiration_AcceptsFutureDate()
    {
        Assert.Null(ShortUrlRules.ValidateExpiration(Now.AddSeconds(1), Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-86_400)]
    public void ValidateExpiration_RejectsCurrentOrPastDate(int offsetSeconds)
    {
        Assert.Equal("The expiration date must be in the future.",
            ShortUrlRules.ValidateExpiration(Now.AddSeconds(offsetSeconds), Now));
    }

    private static string UrlOfLength(int length)
    {
        const string prefix = "https://example.com/";
        return prefix + new string('a', length - prefix.Length);
    }
}
