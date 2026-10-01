using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UrlShortener.Application.Abstractions;
using UrlShortener.Application.Common;
using UrlShortener.Application.Options;
using UrlShortener.Application.ShortCodes;
using UrlShortener.Domain.ShortUrls;

namespace UrlShortener.Application.ShortUrls;

/// <summary>Use cases for creating and reading short links. Knows nothing about HTTP.</summary>
public sealed class ShortUrlService(
    IShortUrlRepository repository,
    IShortCodeGenerator codeGenerator,
    IOptions<UrlShortenerOptions> options,
    TimeProvider timeProvider,
    ILogger<ShortUrlService> logger)
{
    /// <summary>
    /// Codes that would shadow the service's own routes. ASP.NET Core matches literal route segments
    /// case-insensitively, so the comparison is case-insensitive too.
    /// </summary>
    private static readonly HashSet<string> ReservedCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "swagger", "openapi", "health",
    };

    private readonly UrlShortenerOptions _options = options.Value;

    public async Task<ShortUrlResponse> CreateAsync(CreateShortUrlRequest request, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        Validate(request, now);

        var expiresAt = request.ExpiresAt?.UtcDateTime ?? (now + _options.DefaultLifetime);

        var shortUrl = request.CustomCode is { } customCode
            ? await CreateWithCustomCodeAsync(request, customCode, now, expiresAt, cancellationToken)
            : await CreateWithGeneratedCodeAsync(request, now, expiresAt, cancellationToken);

        // The full destination URL is not logged: query strings often carry tokens or personal data.
        logger.LogInformation(
            "Short URL {ShortCode} created for host {Host} (expires {ExpiresAt})",
            shortUrl.ShortCode, new Uri(shortUrl.OriginalUrl).Host, shortUrl.ExpiresAt?.ToString("O") ?? "never");

        return ToResponse(shortUrl);
    }

    public async Task<ShortUrlResponse> GetAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        EnsureValidShortCode(shortCode);

        var shortUrl = await repository.GetByShortCodeAsync(shortCode, cancellationToken)
            ?? throw new ShortUrlNotFoundException(shortCode);

        return ToResponse(shortUrl);
    }

    internal ShortUrlResponse ToResponse(ShortUrl shortUrl) => new(
        shortUrl.ShortCode,
        _options.BuildShortUrl(shortUrl.ShortCode),
        shortUrl.OriginalUrl,
        shortUrl.CreatedAt,
        shortUrl.ExpiresAt,
        shortUrl.IsActive,
        shortUrl.CreatedBy);

    internal static void EnsureValidShortCode(string shortCode)
    {
        if (ShortUrlRules.ValidateShortCode(shortCode) is { } error)
        {
            throw new RequestValidationException(new Dictionary<string, string[]> { ["shortCode"] = [error] });
        }
    }

    private async Task<ShortUrl> CreateWithCustomCodeAsync(
        CreateShortUrlRequest request, string customCode, DateTime now, DateTime? expiresAt, CancellationToken cancellationToken)
    {
        // Cheap pre-check for a clear error message. The unique index still has the final word,
        // because another request may take the code between this check and the insert.
        if (await repository.ShortCodeExistsAsync(customCode, cancellationToken))
        {
            throw new ShortCodeConflictException(customCode);
        }

        var shortUrl = ShortUrl.Create(request.Url, customCode, now, expiresAt, request.CreatedBy);

        try
        {
            await repository.AddAsync(shortUrl, cancellationToken);
        }
        catch (DuplicateShortCodeException)
        {
            throw new ShortCodeConflictException(customCode);
        }

        return shortUrl;
    }

    private async Task<ShortUrl> CreateWithGeneratedCodeAsync(
        CreateShortUrlRequest request, DateTime now, DateTime? expiresAt, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= _options.MaxGenerationAttempts; attempt++)
        {
            var code = codeGenerator.Generate(_options.ShortCodeLength);

            if (ReservedCodes.Contains(code) || await repository.ShortCodeExistsAsync(code, cancellationToken))
            {
                logger.LogWarning("Generated short code collided with an existing one (attempt {Attempt})", attempt);
                continue;
            }

            var shortUrl = ShortUrl.Create(request.Url, code, now, expiresAt, request.CreatedBy);

            try
            {
                await repository.AddAsync(shortUrl, cancellationToken);
                return shortUrl;
            }
            catch (DuplicateShortCodeException)
            {
                // A concurrent request inserted the same code after our existence check.
                logger.LogWarning("Generated short code was taken concurrently (attempt {Attempt})", attempt);
            }
        }

        logger.LogError("Could not generate a unique short code after {Attempts} attempts", _options.MaxGenerationAttempts);
        throw new ShortCodeGenerationException(_options.MaxGenerationAttempts);
    }

    private void Validate(CreateShortUrlRequest request, DateTime now)
    {
        var errors = new Dictionary<string, string[]>();

        if (ShortUrlRules.ValidateOriginalUrl(request.Url) is { } urlError)
        {
            errors["url"] = [urlError];
        }
        else if (PointsToThisService(request.Url))
        {
            errors["url"] = ["Short URLs of this service cannot be shortened again."];
        }

        if (ShortUrlRules.ValidateExpiration(request.ExpiresAt?.UtcDateTime, now) is { } expirationError)
        {
            errors["expiresAt"] = [expirationError];
        }

        if (request.CustomCode is { } customCode)
        {
            if (ShortUrlRules.ValidateShortCode(customCode) is { } codeError)
            {
                errors["customCode"] = [codeError];
            }
            else if (ReservedCodes.Contains(customCode))
            {
                errors["customCode"] = [$"The short code '{customCode}' is reserved."];
            }
        }

        if (ShortUrlRules.ValidateCreatedBy(request.CreatedBy) is { } createdByError)
        {
            errors["createdBy"] = [createdByError];
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }
    }

    /// <summary>Prevents redirect loops: a short link pointing to another short link of this service.</summary>
    private bool PointsToThisService(string url) =>
        Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out var baseUri)
        && Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && string.Equals(uri.Authority, baseUri.Authority, StringComparison.OrdinalIgnoreCase);
}
