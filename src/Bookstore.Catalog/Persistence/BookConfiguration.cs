using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookstore.Catalog.Persistence;

internal sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books");
        builder.HasKey(x => x.Isbn);
        builder.Property(x => x.Isbn).HasMaxLength(13);
        builder.Property(x => x.Price).HasPrecision(10, 2);
    }
}
