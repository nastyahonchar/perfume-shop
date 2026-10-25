using Catalog.Domain.Constants;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class PerfumeNoteConfiguration : IEntityTypeConfiguration<PerfumeNote>
{
    public void Configure(EntityTypeBuilder<PerfumeNote> builder)
    {
        builder.ToTable("PerfumeNotes", t =>
            t.HasCheckConstraint("CK_PerfumeNotes_Layer", ConfigurationHelpers.InList("Layer", NoteLayers.All)));

        builder.HasKey(pn => new { pn.PerfumeId, pn.NoteId });

        builder.Property(pn => pn.Layer).IsRequired().HasMaxLength(10);
        builder.Property(pn => pn.AddedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(pn => pn.Perfume)
            .WithMany(p => p.PerfumeNotes)
            .HasForeignKey(pn => pn.PerfumeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pn => pn.Note)
            .WithMany(n => n.PerfumeNotes)
            .HasForeignKey(pn => pn.NoteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(pn => new { pn.NoteId, pn.PerfumeId })
            .IncludeProperties(pn => pn.Layer)
            .HasDatabaseName("IX_PerfumeNotes_NoteId");
    }
}