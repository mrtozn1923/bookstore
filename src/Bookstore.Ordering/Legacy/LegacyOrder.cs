namespace Bookstore.Ordering.Legacy;

public class LegacyCart
{
    public List<LegacyCartLine> Lines { get; set; } = [];
}

public class LegacyOrder
{
    public List<LegacyCartLine> Lines { get; set; } = [];

    public static LegacyOrder PlaceFrom(LegacyCart cart) => new() { Lines = cart.Lines };
}

public record PriceQuery
{
    public string Isbn { get; set; } = "";
    public string Currency { get; set; } = "TRY";
}
