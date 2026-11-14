namespace Bookstore.Gateway.Bff;

// Shaped for the Angular book page; null means "unknown", not "zero" or "no".
public sealed record BookDetailsView(
    string Isbn,
    string Title,
    string Author,
    decimal Price,
    int? Available,
    bool? PurchasedBefore,
    IReadOnlyList<string> Warnings);

internal sealed record CatalogBook(string Isbn, string Title, string Author, decimal Price);

internal sealed record StockLevel(string Isbn, int Available);

internal sealed record Purchase(string Isbn, bool PurchasedBefore, int? LastOrderId);
