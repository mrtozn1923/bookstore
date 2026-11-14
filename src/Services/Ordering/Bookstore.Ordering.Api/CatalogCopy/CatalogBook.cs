namespace Bookstore.Ordering.CatalogCopy;

// Ordering's own copy of the book data it needs. Catalog owns the truth; this copy is filled once
// from Catalog's API and kept current by Catalog's events.
public sealed class CatalogBook
{
    public required string Isbn { get; set; }
    public required string Title { get; set; }
    public decimal Price { get; set; }
    public required string Currency { get; set; }

    // Catalog's book version at the last applied change of each field. One version per field, because
    // a late BookRenamed v4 must still be applied after BookPriceChanged v5 has already arrived.
    public int PriceVersion { get; set; }
    public int TitleVersion { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
