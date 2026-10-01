using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Domain.ShortUrls;
using UrlShortener.Domain.Visits;

namespace UrlShortener.Infrastructure.Persistence.Configurations;

internal sealed class ShortUrlConfiguration : IEntityTypeConfiguration<ShortUrl>
{
    public void Configure(EntityTypeBuilder<ShortUrl> builder)
    {
        builder.ToTable("ShortUrls");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OriginalUrl)
            .HasMaxLength(ShortUrlRules.MaxUrlLength)
            .IsRequired();

        // Codes are case-sensitive ("abc1" and "ABC1" are different links). SQLite and PostgreSQL both
        // compare text case-sensitively by default, so the unique index below enforces exactly that.
        builder.Property(x => x.ShortCode)
            .HasMaxLength(ShortUrlRules.MaxShortCodeLength)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(ShortUrlRules.MaxCreatedByLength);

        // The source of truth against duplicate codes: concurrent requests can both pass the
        // "does this code exist?" check, but only one INSERT can succeed.
        builder.HasIndex(x => x.ShortCode).IsUnique();

        // Administrative listing: filtering by expiration and ordering by creation date.
        builder.HasIndex(x => x.ExpiresAt);
        builder.HasIndex(x => x.CreatedAt);

        builder.HasMany<UrlVisit>()
            .WithOne()
            .HasForeignKey(v => v.ShortUrlId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
