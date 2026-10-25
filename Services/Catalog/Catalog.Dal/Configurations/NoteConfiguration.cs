using Catalog.Domain.Constants;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.ToTable("Notes", t =>
            t.HasCheckConstraint("CK_Notes_Family", ConfigurationHelpers.InList("Family", NoteFamilies.All)));

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Name).IsRequired().HasMaxLength(60);
        builder.Property(n => n.Family).IsRequired().HasMaxLength(20);

        builder.HasIndex(n => n.Name).IsUnique().HasDatabaseName("UQ_Notes_Name");

        builder.HasIndex(n => n.Family)
            .IncludeProperties(n => n.Name)
            .HasDatabaseName("IX_Notes_Family");

        builder.HasData(
            new Note { Id = 1, Name = "Bergamot", Family = NoteFamilies.Citrus },
            new Note { Id = 2, Name = "Lemon", Family = NoteFamilies.Citrus },
            new Note { Id = 3, Name = "Grapefruit", Family = NoteFamilies.Citrus },
            new Note { Id = 4, Name = "Cherry", Family = NoteFamilies.Fruity },
            new Note { Id = 5, Name = "Apple", Family = NoteFamilies.Fruity },
            new Note { Id = 6, Name = "Fig", Family = NoteFamilies.Fruity },
            new Note { Id = 7, Name = "Pumpkin", Family = NoteFamilies.Gourmand },
            new Note { Id = 8, Name = "Vanilla", Family = NoteFamilies.Gourmand },
            new Note { Id = 9, Name = "Caramel", Family = NoteFamilies.Gourmand },
            new Note { Id = 10, Name = "Coffee", Family = NoteFamilies.Gourmand },
            new Note { Id = 11, Name = "Cinnamon", Family = NoteFamilies.Spicy },
            new Note { Id = 12, Name = "Clove", Family = NoteFamilies.Spicy },
            new Note { Id = 13, Name = "Cardamom", Family = NoteFamilies.Spicy },
            new Note { Id = 14, Name = "Pink Pepper", Family = NoteFamilies.Spicy },
            new Note { Id = 15, Name = "Rose", Family = NoteFamilies.Floral },
            new Note { Id = 16, Name = "Iris", Family = NoteFamilies.Floral },
            new Note { Id = 17, Name = "Jasmine", Family = NoteFamilies.Floral },
            new Note { Id = 18, Name = "Cherry Blossom", Family = NoteFamilies.Floral },
            new Note { Id = 19, Name = "Cedar", Family = NoteFamilies.Woody },
            new Note { Id = 20, Name = "Sandalwood", Family = NoteFamilies.Woody },
            new Note { Id = 21, Name = "Patchouli", Family = NoteFamilies.Woody },
            new Note { Id = 22, Name = "Oud", Family = NoteFamilies.Woody },
            new Note { Id = 23, Name = "Vetiver", Family = NoteFamilies.Woody },
            new Note { Id = 24, Name = "Amber", Family = NoteFamilies.Amber },
            new Note { Id = 25, Name = "Musk", Family = NoteFamilies.Musk },
            new Note { Id = 26, Name = "Sea Salt", Family = NoteFamilies.Aquatic },
            new Note { Id = 27, Name = "Mint", Family = NoteFamilies.Green },
            new Note { Id = 28, Name = "Smoke", Family = NoteFamilies.Smoky });
    }
}