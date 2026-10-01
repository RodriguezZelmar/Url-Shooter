using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using UrlShortener.Application.Redirects;

namespace UrlShortener.Api.Controllers;

/// <summary>Public entry point of every short link: <c>GET /{shortCode}</c>.</summary>
[ApiController]
[Tags("Redirect")]
public sealed class RedirectController(RedirectService redirectService) : ControllerBase
{
    /// <summary>Redirects to the original URL.</summary>
    /// <remarks>
    /// Answers <c>302 Found</c> with the original URL in the <c>Location</c> header.
    /// A temporary redirect is used on purpose: browsers and proxies do not cache it, so every visit
    /// reaches the service (and is counted), and the link can later be deactivated or expire.
    ///
    /// Swagger UI cannot display the redirect (the browser follows it and the destination usually blocks
    /// the cross-origin request); open the short URL in the address bar or use <c>curl -i</c>.
    /// </remarks>
    /// <param name="shortCode">The short code, for example <c>a8F2k9Q</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="302">Redirect to the original URL (<c>Location</c> header).</response>
    /// <response code="404">The code does not exist or the link has been deactivated.</response>
    [HttpGet("/{shortCode:regex(^[[a-zA-Z0-9]]{{4,32}}$)}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> RedirectToOriginal(string shortCode, CancellationToken cancellationToken)
    {
        var originalUrl = await redirectService.ResolveAsync(shortCode, cancellationToken);

        // 302 is not cacheable by default; "no-store" makes that explicit for intermediaries.
        Response.Headers[HeaderNames.CacheControl] = "no-store";

        return Redirect(originalUrl);
    }
}
