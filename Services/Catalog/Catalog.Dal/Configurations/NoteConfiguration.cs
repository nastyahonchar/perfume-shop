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
    }
}