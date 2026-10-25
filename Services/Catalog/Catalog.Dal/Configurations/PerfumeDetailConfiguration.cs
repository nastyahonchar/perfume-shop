using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class PerfumeDetailConfiguration : IEntityTypeConfiguration<PerfumeDetail>
{
    public void Configure(EntityTypeBuilder<PerfumeDetail> builder)
    {
        builder.ToTable("PerfumeDetails", t =>
        {
            t.HasCheckConstraint("CK_PerfumeDetails_Year", "[LaunchYear] BETWEEN 1800 AND 2100");
            t.HasCheckConstraint("CK_PerfumeDetails_Longevity", "[Longevity] BETWEEN 1 AND 5");
        });

        builder.HasKey(d => d.PerfumeId);

        builder.Property(d => d.Description).IsRequired().HasMaxLength(2000);
        builder.Property(d => d.Perfumer).HasMaxLength(100);
    }
}