namespace Bookstore.Data.Entities;

// Keyless read model defined by a SQL query (ToSqlQuery).
public class BookSalesSummary
{
    public int BookId { get; set; }
    public string Title { get; set; } = null!;
    public int Units { get; set; }
}
