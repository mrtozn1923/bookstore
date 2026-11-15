using System.Globalization;

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

    public static string ReviewsHeading(string lang) => lang == "en" ? "Reader reviews" : "Okur yorumları";

    // The rating is already rounded to one decimal; "F1" only changes the separator
    public static string RatingSummary(decimal value, int ratings, int reviews, string lang) => lang == "en"
        ? $"{value.ToString("F1", CultureInfo.GetCultureInfo("en-US"))} / 5 · {ratings} ratings, {reviews} reviews"
        : $"{value.ToString("F1", CultureInfo.GetCultureInfo("tr-TR"))} / 5 · {ratings} puan, {reviews} yorum";

    public static string IncentiveNote(string lang) => lang == "en"
        ? "The reviewer received a free copy from Bookstore."
        : "Yorumcu kitabın ücretsiz bir kopyasını Bookstore'dan aldı.";
}
