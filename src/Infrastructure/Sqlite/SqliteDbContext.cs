using Microsoft.EntityFrameworkCore;
using Sqlite.Documents;

namespace Sqlite;

/// <summary>
/// Контекст файла SQLite: сущности хранятся документами, а выборка идёт по отдельным колонкам.
/// </summary>
public class SqliteDbContext(DbContextOptions<SqliteDbContext> options) : DbContext(options)
{
    public DbSet<MarkRow> Marks => Set<MarkRow>();

    public DbSet<DocumentRow> Documents => Set<DocumentRow>();

    public DbSet<MarkCheckingStatisticRow> MarkCheckingStatistic => Set<MarkCheckingStatisticRow>();

    public DbSet<BeerOnTapRow> BeerOnTap => Set<BeerOnTapRow>();

    public DbSet<GisMtDocumentRow> GisMtDocuments => Set<GisMtDocumentRow>();

    public DbSet<GisMtMarkRow> GisMtMarks => Set<GisMtMarkRow>();

    public DbSet<GtinCatalogRow> GtinCatalog => Set<GtinCatalogRow>();

    public DbSet<ImportProgressRow> ImportProgress => Set<ImportProgressRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MarkRow>(entity =>
        {
            entity.ToTable("Marks");
            entity.HasKey(row => row.Id);
            entity.HasIndex(row => row.MarkId);
            entity.HasIndex(row => row.ReqTimestamp);
        });

        modelBuilder.Entity<DocumentRow>(entity =>
        {
            entity.ToTable("Documents");
            entity.HasKey(row => row.Id);
        });

        modelBuilder.Entity<MarkCheckingStatisticRow>(entity =>
        {
            entity.ToTable("MarkCheckingStatistic");
            entity.HasKey(row => row.Id);
            entity.HasIndex(row => row.SGtin);
            entity.HasIndex(row => row.CheckDay);
            entity.HasIndex(row => row.CheckDate);
        });

        modelBuilder.Entity<BeerOnTapRow>(entity =>
        {
            entity.ToTable("BeerOnTap");
            entity.HasKey(row => row.Id);
        });

        modelBuilder.Entity<GisMtDocumentRow>(entity =>
        {
            entity.ToTable("GisMtDocuments");
            entity.HasKey(row => row.Id);
            entity.HasIndex(row => row.Number);
            entity.HasIndex(row => row.LoadedAt);
        });

        modelBuilder.Entity<GisMtMarkRow>(entity =>
        {
            entity.ToTable("GisMtMarks");
            entity.HasKey(row => row.Id);
            entity.HasIndex(row => row.SGtin);
            entity.HasIndex(row => row.Cis);
            entity.HasIndex(row => row.ProductGroup);
            entity.HasIndex(row => row.InfoLoadedAt);
            entity.HasIndex(row => row.Sold);
            entity.HasIndex(row => row.ExpireDate);
        });

        modelBuilder.Entity<GtinCatalogRow>(entity =>
        {
            entity.ToTable("GtinCatalog");
            entity.HasKey(row => row.Id);
            entity.HasIndex(row => row.Gtin);
        });

        modelBuilder.Entity<ImportProgressRow>(entity =>
        {
            entity.ToTable("ImportProgress");
            entity.HasKey(row => row.Id);
        });
    }
}
