using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("Brands", t =>
            t.HasCheckConstraint("CK_Brands_Year", "[FoundedYear] BETWEEN 1700 AND 2100"));

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name).IsRequired().HasMaxLength(100);
        builder.Property(b => b.Country).HasMaxLength(60);

        builder.HasIndex(b => b.Name).IsUnique().HasDatabaseName("UQ_Brands_Name");

        builder.HasData(
            new Brand { Id = 1, Name = "Maison Aurelle", Country = "France", FoundedYear = 1952 },
            new Brand { Id = 2, Name = "Nordic Bloom", Country = "Sweden", FoundedYear = 2009 },
            new Brand { Id = 3, Name = "Velvet Atelier", Country = "Italy", FoundedYear = 1987 },
            new Brand { Id = 4, Name = "Casa Ambra", Country = "Spain", FoundedYear = 1999 },
            new Brand { Id = 5, Name = "Oud & Co", Country = "UAE", FoundedYear = 2014 });
    }
}