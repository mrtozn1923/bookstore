using Bookstore.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookstore.Data.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> b)
    {
        b.ToTable("Books", "catalog", t => t.HasCheckConstraint("CK_Books_Price", "[Price] >= 0"));

        b.Property(x => x.Title).UseCollation("Turkish_CI_AS");
        b.Property(x => x.Slug).HasMaxLength(220).IsUnicode(false);
        b.Property(x => x.Isbn)
            .HasMaxLength(13)
            .IsFixedLength()
            .IsUnicode(false)
            .HasComment("ISBN-13 without dashes");
        b.Property(x => x.Price).HasPrecision(10, 2);

        b.HasAlternateKey(x => x.Isbn);
        b.HasIndex(x => x.Title);
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => x.PublishedOn)
            .IsDescending()
            .IncludeProperties(x => new { x.Title, x.Price })
            .HasDatabaseName("IX_Books_PublishedOn_Newest");
    }
}
