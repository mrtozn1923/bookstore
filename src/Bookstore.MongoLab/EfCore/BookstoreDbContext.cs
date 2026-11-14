using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Metadata.Conventions;

namespace Bookstore.MongoLab.EfCore;

public sealed class BookstoreDbContext(DbContextOptions<BookstoreDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Field names in our documents are camelCase (post 2); without this EF Core writes "ListPrice"
        configurationBuilder.Conventions.Add(_ => new CamelCaseElementNameConvention());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(book =>
        {
            book.ToCollection("books");
            book.OwnsMany(b => b.Authors);
            book.OwnsOne(b => b.Category);
            book.OwnsOne(b => b.Stock);
            // A navigation to another collection: Include becomes a $lookup (provider 10.0.3+)
            book.HasMany(b => b.Reviews).WithOne().HasForeignKey(r => r.BookId);
        });

        modelBuilder.Entity<Review>(review =>
        {
            review.ToCollection("reviews");
            review.OwnsOne(r => r.Customer);
        });

        modelBuilder.Entity<Order>(order =>
        {
            order.ToCollection("orders");
            order.OwnsOne(o => o.Customer);
            order.OwnsMany(o => o.Items);
            order.OwnsMany(o => o.Payments);
            order.Property(o => o.Status).HasConversion<string>();
        });
    }
}

public static class BookstoreEfCore
{
    // The DbContext is scoped (one change tracker per request); the MongoClient underneath stays a singleton
    public static IServiceCollection AddBookstoreEfCore(this IServiceCollection services) =>
        services.AddDbContext<BookstoreDbContext>((sp, options) =>
            options.UseMongoDB(sp.GetRequiredService<IMongoClient>(), "bookstore"));
}
