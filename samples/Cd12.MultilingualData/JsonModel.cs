using Microsoft.EntityFrameworkCore;

namespace Bookstore.Catalog.Localization.Json;

// Option C: translations in a json column (SQL Server 2025, compatibility level 170)
public class InstitutionUnit
{
    public int Id { get; set; }
    public LocalizedText Name { get; set; } = new();
}

public class LocalizedText
{
    public string Tr { get; set; } = "";
    public string? En { get; set; }
}

public class JsonContext(DbContextOptions<JsonContext> options) : DbContext(options)
{
    public DbSet<InstitutionUnit> InstitutionUnits => Set<InstitutionUnit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.Entity<InstitutionUnit>().ComplexProperty(u => u.Name, n => n.ToJson());
}
