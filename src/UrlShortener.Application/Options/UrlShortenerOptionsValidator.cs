using Microsoft.Extensions.Options;
using UrlShortener.Domain.ShortUrls;

namespace UrlShortener.Application.Options;

/// <summary>Fails application startup when <see cref="UrlShortenerOptions"/> is misconfigured.</summary>
public sealed class UrlShortenerOptionsValidator : IValidateOptions<UrlShortenerOptions>
{
    public const int MaxGenerationAttemptsLimit = 20;

    public ValidateOptionsResult Validate(string? name, UrlShortenerOptions options)
    {
        var errors = new List<string>();

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUrl)
            || (baseUrl.Scheme != Uri.UriSchemeHttp && baseUrl.Scheme != Uri.UriSchemeHttps))
        {
            errors.Add($"{UrlShortenerOptions.SectionName}:BaseUrl must be an absolute http or https URL.");
        }

        if (options.ShortCodeLength is < ShortUrlRules.MinShortCodeLength or > ShortUrlRules.MaxShortCodeLength)
        {
            errors.Add($"{UrlShortenerOptions.SectionName}:ShortCodeLength must be between " +
                $"{ShortUrlRules.MinShortCodeLength} and {ShortUrlRules.MaxShortCodeLength}.");
        }

        if (options.MaxGenerationAttempts is < 1 or > MaxGenerationAttemptsLimit)
        {
            errors.Add($"{UrlShortenerOptions.SectionName}:MaxGenerationAttempts must be between 1 and {MaxGenerationAttemptsLimit}.");
        }

        if (options.DefaultLifetime is { } lifetime && lifetime <= TimeSpan.Zero)
        {
            errors.Add($"{UrlShortenerOptions.SectionName}:DefaultLifetime must be a positive duration.");
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
