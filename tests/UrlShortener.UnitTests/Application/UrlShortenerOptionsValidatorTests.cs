using UrlShortener.Application.Options;

namespace UrlShortener.UnitTests.Application;

public sealed class UrlShortenerOptionsValidatorTests
{
    private readonly UrlShortenerOptionsValidator _validator = new();

    [Fact]
    public void Validate_AcceptsDefaultsWithBaseUrl()
    {
        var result = _validator.Validate(null, new UrlShortenerOptions { BaseUrl = "http://localhost:5000" });

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("localhost:5000")]
    [InlineData("ftp://files.example.com")]
    public void Validate_RejectsInvalidBaseUrl(string baseUrl)
    {
        var result = _validator.Validate(null, new UrlShortenerOptions { BaseUrl = baseUrl });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("BaseUrl"));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(33)]
    public void Validate_RejectsShortCodeLengthOutsideDomainLimits(int length)
    {
        var result = _validator.Validate(null, new UrlShortenerOptions { BaseUrl = "https://sho.rt", ShortCodeLength = length });

        Assert.Contains(result.Failures!, f => f.Contains("ShortCodeLength"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Validate_RejectsUnreasonableMaxGenerationAttempts(int attempts)
    {
        var result = _validator.Validate(null, new UrlShortenerOptions { BaseUrl = "https://sho.rt", MaxGenerationAttempts = attempts });

        Assert.Contains(result.Failures!, f => f.Contains("MaxGenerationAttempts"));
    }

    [Fact]
    public void Validate_RejectsNonPositiveDefaultLifetime()
    {
        var result = _validator.Validate(null, new UrlShortenerOptions { BaseUrl = "https://sho.rt", DefaultLifetime = TimeSpan.Zero });

        Assert.Contains(result.Failures!, f => f.Contains("DefaultLifetime"));
    }

    [Fact]
    public void BuildShortUrl_JoinsBaseUrlAndCodeWithASingleSlash()
    {
        Assert.Equal("https://sho.rt/abc123X", new UrlShortenerOptions { BaseUrl = "https://sho.rt/" }.BuildShortUrl("abc123X"));
        Assert.Equal("https://sho.rt/abc123X", new UrlShortenerOptions { BaseUrl = "https://sho.rt" }.BuildShortUrl("abc123X"));
    }
}
