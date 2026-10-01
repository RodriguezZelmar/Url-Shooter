using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Domain.Visits;

namespace UrlShortener.Infrastructure.Persistence.Configurations;

internal sealed class UrlVisitConfiguration : IEntityTypeConfiguration<UrlVisit>
{
    public void Configure(EntityTypeBuilder<UrlVisit> builder)
    {
        builder.ToTable("UrlVisits");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserAgent).HasMaxLength(UrlVisit.MaxUserAgentLength);
        builder.Property(x => x.Referrer).HasMaxLength(UrlVisit.MaxReferrerLength);

        // Every statistics query filters by link and then counts or groups by date. A composite index
        // (ShortUrlId, VisitedAt) serves both, and its leading column also covers the foreign key,
        // so a separate single-column index on ShortUrlId would be redundant.
        builder.HasIndex(x => new { x.ShortUrlId, x.VisitedAt });
    }
}
