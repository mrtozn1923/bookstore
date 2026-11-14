using Microsoft.EntityFrameworkCore;

namespace Bookstore.Catalog.Localization.Naive;

// Option A as it was first written: one column per language, NOT NULL, "" instead of null
public class Category
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string NameTr { get; set; } = "";
    public string NameEn { get; set; } = "";
}

public class NaiveContext(DbContextOptions<NaiveContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
}
