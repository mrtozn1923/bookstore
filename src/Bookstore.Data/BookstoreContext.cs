using Bookstore.Data.Entities;
using Bookstore.Data.Migrations.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Bookstore.Data;

public class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<BookSalesSummary> BookSalesSummaries => Set<BookSalesSummary>();

    public IQueryable<BestSeller> BestSellers(DateTime since)
        => FromExpression(() => BestSellers(since));

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.ReplaceService<IMigrationsSqlGenerator, BookstoreMigrationsSqlGenerator>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<string>().HaveMaxLength(200);
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<int>("OrderNumbers", "ordering")
            .StartsAt(100001)
            .IncrementsBy(1);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookstoreContext).Assembly);

        modelBuilder.Entity<BookSalesSummary>()
            .HasNoKey()
            .ToView("vw_BookSales", "catalog");

        modelBuilder.HasDbFunction(typeof(BookstoreFunctions).GetMethod(nameof(BookstoreFunctions.DiscountedPrice))!)
            .HasName("fn_DiscountedPrice")
            .HasSchema("dbo");

        modelBuilder.Entity<BestSeller>().HasNoKey().ToTable((string?)null);
        modelBuilder.HasDbFunction(typeof(BookstoreContext).GetMethod(nameof(BestSellers), [typeof(DateTime)])!)
            .HasName("fn_BestSellers");
    }
}
