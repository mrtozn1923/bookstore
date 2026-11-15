using System.Globalization;

namespace Bookstore.Ordering.Pricing;

public static class PriceText
{
    public static decimal ParseLegacy(string text) => decimal.Parse(text);

    public static decimal Parse(string text) => decimal.Parse(text, NumberStyles.Number, CultureInfo.InvariantCulture);
}
