using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using UrlShortener.Application.Common;
using UrlShortener.Application.Options;
using UrlShortener.Application.ShortCodes;
using UrlShortener.Application.ShortUrls;
using UrlShortener.Domain.ShortUrls;
using UrlShortener.UnitTests.Fakes;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace UrlShortener.UnitTests.Application;

public sealed class ShortUrlServiceCreateTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryShortUrlRepository _repository = new();
    private readonly FakeTimeProvider _time = new(Now);
    private readonly UrlShortenerOptions _options = new() { BaseUrl = "https://sho.rt", ShortCodeLength = 7, MaxGenerationAttempts = 3 };

    [Fact]
    public async Task CreateAsync_StoresLinkAndReturnsShortUrlBuiltFromBaseUrl()
    {
        var service = CreateService(new SequenceShortCodeGenerator("abc123X"));

        var response = await service.CreateAsync(new CreateShortUrlRequest("https://example.com/long", CreatedBy: "marketing"));

        Assert.Equal("abc123X", response.ShortCode);
        Assert.Equal("https://sho.rt/abc123X", response.ShortUrl);
        Assert.Equal("https://example.com/long", response.OriginalUrl);
        Assert.Equal(Now.UtcDateTime, response.CreatedAt);
        Assert.Null(response.ExpiresAt);
        Assert.True(response.IsActive);
        Assert.Equal("marketing", response.CreatedBy);
        Assert.Single(_repository.Items);
    }

    [Fact]
    public async Task CreateAsync_WithRealGenerator_UsesConfiguredCodeLength()
    {
        _options.ShortCodeLength = 9;
        var service = CreateService(new RandomShortCodeGenerator());

        var response = await service.CreateAsync(new CreateShortUrlRequest("https://example.com"));

        Assert.Equal(9, response.ShortCode.Length);
    }

    [Fact]
    public async Task CreateAsync_ConvertsExpirationToUtc()
    {
        var service = CreateService(new SequenceShortCodeGenerator("abc123X"));
        var expiresAt = new DateTimeOffset(2026, 12, 31, 20, 59, 59, TimeSpan.FromHours(-3));

        var response = await service.CreateAsync(new CreateShortUrlRequest("https://example.com", expiresAt));

        Assert.Equal(new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc), response.ExpiresAt);
        Assert.Equal(DateTimeKind.Utc, response.ExpiresAt!.Value.Kind);
    }

    [Fact]
    public async Task CreateAsync_AppliesDefaultLifetimeWhenNoExpirationIsGiven()
    {
        _options.DefaultLifetime = TimeSpan.FromDays(30);
        var service = CreateService(new SequenceShortCodeGenerator("abc123X"));

        var response = await service.CreateAsync(new CreateShortUrlRequest("https://example.com"));

        Assert.Equal(Now.UtcDateTime.AddDays(30), response.ExpiresAt);
    }

    [Fact]
    public async Task CreateAsync_RetriesWhenGeneratedCodeAlreadyExists()
    {
        _repository.Seed(ShortUrl.Create("https://other.com", "taken01", Now.UtcDateTime, null));
        var generator = new SequenceShortCodeGenerator("taken01", "free001");
        var service = CreateService(generator);

        var response = await service.CreateAsync(new CreateShortUrlRequest("https://example.com"));

        Assert.Equal("free001", response.ShortCode);
        Assert.Equal(2, generator.Calls);
    }

    [Fact]
    public async Task CreateAsync_RetriesWhenDatabaseRejectsCodeTakenConcurrently()
    {
        // The existence check says "free", but the unique constraint rejects the insert.
        _repository.CodesTakenConcurrently.Add("race001");
        var generator = new SequenceShortCodeGenerator("race001", "free001");
        var service = CreateService(generator);

        var response = await service.CreateAsync(new CreateShortUrlRequest("https://example.com"));

        Assert.Equal("free001", response.ShortCode);
        Assert.Equal(2, generator.Calls);
    }

    [Fact]
    public async Task CreateAsync_SkipsReservedCodes()
    {
        var service = CreateService(new SequenceShortCodeGenerator("Swagger", "free001"));

        var response = await service.CreateAsync(new CreateShortUrlRequest("https://example.com"));

        Assert.Equal("free001", response.ShortCode);
    }

    [Fact]
    public async Task CreateAsync_FailsAfterMaxAttemptsOfCollisions()
    {
        _repository.Seed(ShortUrl.Create("https://other.com", "taken01", Now.UtcDateTime, null));
        var generator = new SequenceShortCodeGenerator("taken01", "taken01", "taken01", "never01");
        var service = CreateService(generator);

        await Assert.ThrowsAsync<ShortCodeGenerationException>(
            () => service.CreateAsync(new CreateShortUrlRequest("https://example.com")));

        Assert.Equal(_options.MaxGenerationAttempts, generator.Calls);
    }

    [Fact]
    public async Task CreateAsync_UsesCustomCodeWithoutCallingTheGenerator()
    {
        var generator = new SequenceShortCodeGenerator();
        var service = CreateService(generator);

        var response = await service.CreateAsync(new CreateShortUrlRequest("https://example.com", CustomCode: "promo2026"));

        Assert.Equal("promo2026", response.ShortCode);
        Assert.Equal(0, generator.Calls);
    }

    [Fact]
    public async Task CreateAsync_RejectsCustomCodeAlreadyInUse()
    {
        _repository.Seed(ShortUrl.Create("https://other.com", "promo2026", Now.UtcDateTime, null));
        var service = CreateService(new SequenceShortCodeGenerator());

        await Assert.ThrowsAsync<ShortCodeConflictException>(
            () => service.CreateAsync(new CreateShortUrlRequest("https://example.com", CustomCode: "promo2026")));
    }

    [Fact]
    public async Task CreateAsync_RejectsCustomCodeTakenConcurrently()
    {
        _repository.CodesTakenConcurrently.Add("promo2026");
        var service = CreateService(new SequenceShortCodeGenerator());

        await Assert.ThrowsAsync<ShortCodeConflictException>(
            () => service.CreateAsync(new CreateShortUrlRequest("https://example.com", CustomCode: "promo2026")));
    }

    [Theory]
    [InlineData("swagger")]
    [InlineData("OpenApi")]
    public async Task CreateAsync_RejectsReservedCustomCode(string code)
    {
        var service = CreateService(new SequenceShortCodeGenerator());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.CreateAsync(new CreateShortUrlRequest("https://example.com", CustomCode: code)));

        Assert.Contains("customCode", exception.Errors.Keys);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://example.com")]
    public async Task CreateAsync_RejectsInvalidUrl(string url)
    {
        var service = CreateService(new SequenceShortCodeGenerator());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.CreateAsync(new CreateShortUrlRequest(url)));

        Assert.Contains("url", exception.Errors.Keys);
        Assert.Empty(_repository.Items);
    }

    [Theory]
    [InlineData("https://sho.rt/abc123X")]
    [InlineData("https://SHO.RT/other")]
    public async Task CreateAsync_RejectsUrlsPointingToThisService(string url)
    {
        var service = CreateService(new SequenceShortCodeGenerator());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.CreateAsync(new CreateShortUrlRequest(url)));

        Assert.Equal(["Short URLs of this service cannot be shortened again."], exception.Errors["url"]);
    }

    [Fact]
    public async Task CreateAsync_RejectsExpirationInThePast()
    {
        var service = CreateService(new SequenceShortCodeGenerator());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.CreateAsync(new CreateShortUrlRequest("https://example.com", Now.AddMinutes(-1))));

        Assert.Contains("expiresAt", exception.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_ReportsEveryValidationErrorAtOnce()
    {
        var service = CreateService(new SequenceShortCodeGenerator());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.CreateAsync(new CreateShortUrlRequest("javascript:x", Now.AddDays(-1), "a-b", "")));

        Assert.Equal(["createdBy", "customCode", "expiresAt", "url"], exception.Errors.Keys.Order(StringComparer.Ordinal));
    }

    private ShortUrlService CreateService(IShortCodeGenerator generator) =>
        new(_repository, generator, MsOptions.Create(_options), _time, NullLogger<ShortUrlService>.Instance);
}
