using Microsoft.EntityFrameworkCore;
using Bookstore.Api.Models;

namespace Bookstore.Api.Data;

public class BookstoreContext : DbContext
{
    public BookstoreContext(DbContextOptions<BookstoreContext> options) : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>()
            .Property(b => b.Title)
            .HasMaxLength(100);

        // Seed data is written into the migration
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Science Fiction" },
            new Category { Id = 2, Name = "Classics" });

        modelBuilder.Entity<Book>().HasData(
            new Book { Id = 1, Title = "Dune", Price = 450m, CategoryId = 1 },
            new Book { Id = 2, Title = "Emma", Price = 280m, CategoryId = 2 },
            new Book { Id = 3, Title = "The Hobbit", Price = 320m, CategoryId = 1 });
    }
}
