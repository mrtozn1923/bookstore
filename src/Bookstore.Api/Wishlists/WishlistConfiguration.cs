using Bookstore.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookstore.Wishlists;

internal sealed class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> wishlist)
    {
        wishlist.ToTable("Wishlists", "wishlists");
        wishlist.HasMany(w => w.Items).WithOne().HasForeignKey(i => i.WishlistId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> item)
    {
        item.ToTable("WishlistItems", "wishlists");
        item.HasIndex(i => new { i.WishlistId, i.BookId }).IsUnique();
        item.HasOne<Book>().WithMany().HasForeignKey(i => i.BookId).OnDelete(DeleteBehavior.Restrict);
    }
}
