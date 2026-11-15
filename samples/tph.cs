#:package Microsoft.EntityFrameworkCore.SqlServer@10.0.15
#:property PublishAot=false
using Microsoft.EntityFrameworkCore;

// How the legacy PrintedBook/EBook hierarchy would be stored with TPH (no database needed)
using LegacyCatalogContext db = new();
Console.WriteLine(db.Database.GenerateCreateScript());

public abstract class Book
{
    public int Id { get; set; }
    public string Isbn { get; set; } = "";
    public string Title { get; set; } = "";
    public decimal Price { get; set; }
}

public class PrintedBook : Book
{
    public int WeightGrams { get; set; }
}

public class EBook : Book
{
    public string FileFormat { get; set; } = "";
}

public class LegacyCatalogContext : DbContext
{
    public DbSet<Book> Books => Set<Book>();

    protected override void OnConfiguring(DbContextOptionsBuilder options) =>
        options.UseSqlServer("Server=localhost;Database=BookstoreDb;Integrated Security=true");

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<PrintedBook>();
        model.Entity<EBook>();
        model.Entity<Book>().Property(b => b.Price).HasPrecision(10, 2);
        model.Entity<Book>().Property(b => b.Isbn).HasMaxLength(13);
    }
}
