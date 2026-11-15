using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookstore.Catalog;

internal sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> book)
    {
        book.ToTable("Books", "catalog");
        book.Property(b => b.Isbn).HasMaxLength(13).IsUnicode(false);
        book.HasIndex(b => b.Isbn).IsUnique();
        book.Property(b => b.Title).HasMaxLength(200);
        book.Property(b => b.Subtitle).HasMaxLength(200);
        book.Property(b => b.Description).HasMaxLength(1000);
        book.Property(b => b.Price).HasPrecision(10, 2);
        book.HasOne(b => b.Author).WithMany(a => a.Books).HasForeignKey(b => b.AuthorId);
        book.HasOne(b => b.Category).WithMany().HasForeignKey(b => b.CategoryId);
        book.HasData(CatalogSeed.Books);
    }
}

internal sealed class AuthorConfiguration : IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> author)
    {
        author.ToTable("Authors", "catalog");
        author.Property(a => a.Name).HasMaxLength(200);
        author.HasData(CatalogSeed.Authors);
    }
}

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> category)
    {
        category.ToTable("Categories", "catalog");
        category.Property(c => c.Name).HasMaxLength(100);
        category.Property(c => c.Slug).HasMaxLength(100).IsUnicode(false);
        category.HasIndex(c => c.Slug).IsUnique();
        category.HasData(CatalogSeed.Categories);
    }
}
