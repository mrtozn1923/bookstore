using Microsoft.EntityFrameworkCore;

namespace Bookstore.Catalog.Localization;

public class Category
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public List<CategoryTranslation> Translations { get; set; } = [];
}

public class CategoryTranslation
{
    public int CategoryId { get; set; }
    public string Culture { get; set; } = "";
    public string Name { get; set; } = "";
}

public sealed record CategoryDto(int Id, string Code, string Name, string Culture);

public class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryTranslation> CategoryTranslations => Set<CategoryTranslation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(b =>
        {
            b.Property(c => c.Code).HasMaxLength(20).IsUnicode(false);
            b.HasIndex(c => c.Code).IsUnique();
            b.HasMany(c => c.Translations).WithOne().HasForeignKey(t => t.CategoryId);
        });
        modelBuilder.Entity<CategoryTranslation>(b =>
        {
            b.HasKey(t => new { t.CategoryId, t.Culture });
            b.Property(t => t.Culture).HasMaxLength(10).IsUnicode(false);
            b.Property(t => t.Name).HasMaxLength(100);
            b.HasIndex(t => new { t.Culture, t.Name }).IsUnique();
            b.ToTable(t => t.HasCheckConstraint("CK_CategoryTranslations_Name", "LEN([Name]) > 0"));
        });
    }
}
