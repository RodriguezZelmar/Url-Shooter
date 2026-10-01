using Microsoft.AspNetCore.Mvc;
using UrlShortener.Application.ShortUrls;
using UrlShortener.Application.Statistics;

namespace UrlShortener.Api.Controllers;

/// <summary>Create, inspect and measure short URLs.</summary>
[ApiController]
[Route("api/urls")]
[Produces("application/json")]
public sealed class ShortUrlsController(ShortUrlService shortUrlService, UrlStatisticsService statisticsService) : ControllerBase
{
    /// <summary>Shortens a URL.</summary>
    /// <remarks>
    /// Generates a random short code unless <c>customCode</c> is supplied. Only absolute http/https URLs are accepted.
    ///
    ///     POST /api/urls
    ///     {
    ///       "url": "https://example.com/a/very/long/url",
    ///       "expiresAt": "2026-12-31T23:59:59Z"
    ///     }
    /// </remarks>
    /// <response code="201">The short URL was created. The <c>Location</c> header points to its details.</response>
    /// <response code="400">The URL, expiration date or custom code is invalid.</response>
    /// <response code="409">The requested custom code is already in use.</response>
    [HttpPost]
    [ProducesResponseType<ShortUrlResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<ShortUrlResponse>> Create(CreateShortUrlRequest request, CancellationToken cancellationToken)
    {
        var response = await shortUrlService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(Get), new { shortCode = response.ShortCode }, response);
    }

    /// <summary>Returns the details of a short URL.</summary>
    /// <param name="shortCode">The short code, for example <c>a8F2k9Q</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The short URL.</response>
    /// <response code="400">The short code has an invalid format.</response>
    /// <response code="404">No short URL exists with that code.</response>
    [HttpGet("{shortCode}")]
    [ProducesResponseType<ShortUrlResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public Task<ShortUrlResponse> Get(string shortCode, CancellationToken cancellationToken) =>
        shortUrlService.GetAsync(shortCode, cancellationToken);

    /// <summary>Returns the usage statistics of a short URL.</summary>
    /// <remarks>
    /// <c>totalVisits</c> and <c>lastVisitAt</c> cover the whole life of the link. <c>visitsByDay</c> and
    /// <c>topReferrers</c> cover the period <c>[from, to]</c> (UTC days, both inclusive), which defaults to the
    /// last 30 days and can be at most 366 days long. Days without visits are omitted from <c>visitsByDay</c>.
    /// Statistics are available for inactive and expired links too.
    /// </remarks>
    /// <param name="shortCode">The short code, for example <c>a8F2k9Q</c>.</param>
    /// <param name="from">First UTC day of the period (yyyy-MM-dd). Defaults to 29 days before <paramref name="to"/>.</param>
    /// <param name="to">Last UTC day of the period (yyyy-MM-dd). Defaults to today.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The statistics.</response>
    /// <response code="400">Invalid short code or period.</response>
    /// <response code="404">No short URL exists with that code.</response>
    [HttpGet("{shortCode}/stats")]
    [ProducesResponseType<UrlStatisticsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public Task<UrlStatisticsResponse> GetStatistics(
        string shortCode, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        statisticsService.GetAsync(shortCode, from, to, cancellationToken);
}
