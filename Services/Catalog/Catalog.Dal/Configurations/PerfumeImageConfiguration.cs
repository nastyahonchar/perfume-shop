using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class PerfumeImageConfiguration : IEntityTypeConfiguration<PerfumeImage>
{
    public void Configure(EntityTypeBuilder<PerfumeImage> builder)
    {
        builder.ToTable("PerfumeImages");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Url).IsRequired().HasMaxLength(500);

        builder.HasIndex(i => new { i.PerfumeId, i.SortOrder }).HasDatabaseName("IX_PerfumeImages_Perfume_Sort");

        builder.HasIndex(i => i.PerfumeId)
            .IsUnique()
            .HasFilter("[IsMain] = 1")
            .HasDatabaseName("UX_PerfumeImages_OneMain");
    }
}