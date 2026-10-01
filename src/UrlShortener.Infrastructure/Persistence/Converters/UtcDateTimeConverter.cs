using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace UrlShortener.Infrastructure.Persistence.Converters;

/// <summary>
/// SQLite has no native date type and returns values with <see cref="DateTimeKind.Unspecified"/>.
/// This converter marks values read from the database as UTC, so API responses always carry
/// the "Z" suffix and PostgreSQL's <c>timestamptz</c> always receives UTC values.
/// </summary>
internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime(),
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
