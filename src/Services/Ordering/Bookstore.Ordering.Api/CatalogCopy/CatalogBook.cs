namespace Bookstore.Ordering.CatalogCopy;

// Ordering's own copy of the book data it needs. Catalog owns the truth; this copy is filled once
// from Catalog's API and kept current by Catalog's events (post 06).
public sealed class CatalogBook
{
    public required string Isbn { get; set; }
    public required string Title { get; set; }
    public decimal Price { get; set; }
    public required string Currency { get; set; }
    public int Version { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
