using System.ComponentModel.DataAnnotations;
using UrlShortener.Domain.ShortUrls;

namespace UrlShortener.Application.ShortUrls;

public enum ShortUrlStatus
{
    Active,
    Inactive,
}

/// <summary>Filters and paging for the administrative listing. Every filter is optional and they are combined with AND.</summary>
public sealed class ListShortUrlsQuery
{
    public const int MaxPageSize = 100;

    /// <summary>1-based page number.</summary>
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    /// <summary>Items per page (1-100).</summary>
    [Range(1, MaxPageSize)]
    public int PageSize { get; init; } = 20;

    /// <summary>Only links whose short code starts with this value (case-sensitive).</summary>
    [MaxLength(ShortUrlRules.MaxShortCodeLength)]
    public string? ShortCode { get; init; }

    /// <summary>Only active or only inactive links.</summary>
    public ShortUrlStatus? Status { get; init; }

    /// <summary><c>true</c>: only expired links. <c>false</c>: only links that have not expired (including those without expiration).</summary>
    public bool? Expired { get; init; }

    /// <summary>Only links created with this exact <c>createdBy</c> label.</summary>
    [MaxLength(ShortUrlRules.MaxCreatedByLength)]
    public string? CreatedBy { get; init; }
}

/// <summary>A page of results.</summary>
/// <param name="Items">The items of the requested page, newest first.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Requested page size.</param>
/// <param name="TotalCount">Number of items matching the filters, across all pages.</param>
/// <param name="TotalPages">Number of pages.</param>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

/// <summary>Changes to an existing short URL.</summary>
/// <param name="IsActive">false disables the link (redirects answer 404); true enables it again.</param>
public sealed record UpdateShortUrlRequest([Required] bool? IsActive);
