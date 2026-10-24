using Catalog.Domain.Constants;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class PerfumeConfiguration : IEntityTypeConfiguration<Perfume>
{
    public void Configure(EntityTypeBuilder<Perfume> builder)
    {
        builder.ToTable("Perfumes", t =>
        {
            t.HasCheckConstraint("CK_Perfumes_Price", "[Price] > 0");
            t.HasCheckConstraint("CK_Perfumes_Volume", "[VolumeMl] > 0");
            t.HasCheckConstraint("CK_Perfumes_Gender", ConfigurationHelpers.InList("Gender", Genders.All));
            t.HasCheckConstraint("CK_Perfumes_Concentration", ConfigurationHelpers.InList("Concentration", Concentrations.All));
            t.HasCheckConstraint("CK_Perfumes_Season", ConfigurationHelpers.InList("Season", Seasons.All));
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Sku).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Gender).IsRequired().HasMaxLength(10);
        builder.Property(p => p.Concentration).IsRequired().HasMaxLength(10);
        builder.Property(p => p.Season).IsRequired().HasMaxLength(20).HasDefaultValue(Seasons.AllYear);
        builder.Property(p => p.Price).HasPrecision(18, 2).HasColumnType("decimal(18,2)");
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(p => p.CreatedBy).HasMaxLength(100);
        builder.Property(p => p.UpdatedBy).HasMaxLength(100);
        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.HasIndex(p => p.Sku).IsUnique().HasDatabaseName("UQ_Perfumes_Sku");
        builder.HasIndex(p => new { p.BrandId, p.Name, p.VolumeMl }).IsUnique().HasDatabaseName("UQ_Perfumes_Brand_Name_Volume");

        builder.HasIndex(p => p.BrandId)
            .IncludeProperties(p => new { p.Name, p.Price })
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Perfumes_BrandId");

        builder.HasIndex(p => p.Season)
            .IncludeProperties(p => new { p.Name, p.Price, p.BrandId })
            .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0")
            .HasDatabaseName("IX_Perfumes_Season");

        builder.HasIndex(p => p.Name).HasDatabaseName("IX_Perfumes_Name");

        builder.HasIndex(p => new { p.Gender, p.Price })
            .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0")
            .HasDatabaseName("IX_Perfumes_Gender_Price");

        builder.HasOne(p => p.Brand)
            .WithMany(b => b.Perfumes)
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Detail)
            .WithOne()
            .HasForeignKey<PerfumeDetail>(d => d.PerfumeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Images)
            .WithOne()
            .HasForeignKey(i => i.PerfumeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}