namespace Bookstore.Web.Catalog;

// Visible labels; the JSON-LD uses the same strings so the two cannot drift apart
public static class BookTexts
{
    public static string FormatName(BookFormat format, string lang) => (format, lang) switch
    {
        (BookFormat.Paperback, "en") => "Paperback",
        (BookFormat.Hardcover, "en") => "Hardcover",
        (BookFormat.Paperback, _) => "Karton kapak",
        (BookFormat.Hardcover, _) => "Ciltli",
        _ => format.ToString(),
    };

    public static string Books(string lang) => lang == "en" ? "Books" : "Kitaplar";

    public static string CategoryName(string slug, string lang) => (slug, lang) switch
    {
        ("science-fiction", "en") => "Science Fiction",
        ("science-fiction", _) => "Bilim Kurgu",
        _ => slug,
    };

    public static string Stock(int quantity, string lang) => (quantity > 0, lang) switch
    {
        (true, "en") => "In stock",
        (false, "en") => "Out of stock",
        (true, _) => "Stokta",
        (false, _) => "Tükendi",
    };

    public static string SaleEnds(string lang) => lang == "en" ? "Sale ends" : "Kampanya bitişi";

    public static string AddToCart(string lang) => lang == "en" ? "Add to cart" : "Sepete ekle";
}
