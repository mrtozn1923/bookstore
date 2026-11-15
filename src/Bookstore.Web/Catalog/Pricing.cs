using System.Globalization;

namespace Bookstore.Web.Catalog;

// The single price object that both the visible page and the JSON-LD read
public sealed record PriceQuote(
    decimal Price,
    decimal? StrikethroughPrice,
    DateTimeOffset? SaleStartsAt,
    DateTimeOffset? SaleEndsAt,
    string Currency = "TRY");

public sealed class PricingService(BookCatalog catalog, TimeProvider time)
{
    public PriceQuote Quote(BookEdition book)
    {
        DateTimeOffset now = time.GetUtcNow();
        Campaign? campaign = catalog.Campaigns.FirstOrDefault(c =>
            c.Isbn == book.Isbn && c.StartsAt <= now && now <= c.EndsAt);

        return campaign is null
            ? new PriceQuote(book.ListPrice, null, null, null)
            : new PriceQuote(campaign.Price, book.ListPrice, campaign.StartsAt, campaign.EndsAt);
    }

    public static string Format(decimal amount, string lang) => lang == "en"
        ? $"TRY {amount.ToString("N2", CultureInfo.GetCultureInfo("en-US"))}"
        : $"{amount.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"))} TL";
}
