using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Perfume> Perfumes => Set<Perfume>();
    public DbSet<PerfumeDetail> PerfumeDetails => Set<PerfumeDetail>();
    public DbSet<PerfumeImage> PerfumeImages => Set<PerfumeImage>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<PerfumeNote> PerfumeNotes => Set<PerfumeNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
}