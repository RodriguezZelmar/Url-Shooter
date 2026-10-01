using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using UrlShortener.Application.Options;
using UrlShortener.Application.Redirects;
using UrlShortener.Application.ShortCodes;
using UrlShortener.Application.ShortUrls;

namespace UrlShortener.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the use cases. The host is responsible for binding <see cref="UrlShortenerOptions"/>
    /// (for example from the <c>UrlShortener</c> configuration section); invalid options fail at startup.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddOptions<UrlShortenerOptions>().ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<UrlShortenerOptions>, UrlShortenerOptionsValidator>());

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IShortCodeGenerator, RandomShortCodeGenerator>();
        services.AddScoped<ShortUrlService>();
        services.AddScoped<RedirectService>();

        return services;
    }
}
