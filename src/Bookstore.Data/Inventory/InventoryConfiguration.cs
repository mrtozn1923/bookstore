using Bookstore.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookstore.Inventory;

internal sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> stock)
    {
        stock.ToTable("StockItems", "inventory", t =>
        {
            t.HasCheckConstraint("CK_StockItems_Quantities", "[QuantityReserved] >= 0 AND [QuantityReserved] <= [QuantityOnHand]");
        });
        stock.HasIndex(s => s.BookId).IsUnique();
        // Foreign key in the database, but no navigation property across modules.
        stock.HasOne<Book>().WithOne().HasForeignKey<StockItem>(s => s.BookId);
        stock.Ignore(s => s.Available);

        // 25 copies of every book, except two that make the out-of-stock paths visible.
        stock.HasData(CatalogSeed.Books.Select(b => new StockItem
        {
            Id = b.Id,
            BookId = b.Id,
            QuantityOnHand = b.Isbn switch
            {
                "9780060934347" => 0,  // Don Quixote
                "9781400079988" => 3,  // War and Peace
                _ => 25
            }
        }));
    }
}
