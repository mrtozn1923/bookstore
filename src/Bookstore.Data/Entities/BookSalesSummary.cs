namespace Bookstore.Data.Entities;

// Keyless read model mapped to the catalog.vw_BookSales view.
public class BookSalesSummary
{
    public int BookId { get; set; }
    public string Title { get; set; } = null!;
    public int Units { get; set; }
}
