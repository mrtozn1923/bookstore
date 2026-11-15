namespace Bookstore.Notifications.Documents.Html;

public sealed record QuoteLine(string Isbn, string Title, int Quantity, decimal UnitPrice)
{
    public decimal Amount => Quantity * UnitPrice;
}

// Institutional (B2B) quote: 20+ copies get the 10% institutional discount
public sealed record InstitutionalQuoteModel(
    string QuoteNumber,
    string InstitutionName,
    string UnitName,
    string ContactName,
    DateOnly IssueDate,
    DateOnly ValidUntil,
    IReadOnlyList<QuoteLine> Lines,
    string? SalesNote = null)
{
    public const int MinimumQuantityForDiscount = 20;
    public const decimal InstitutionalDiscountRate = 0.10m;

    public decimal Subtotal => Lines.Sum(l => l.Amount);
    public int TotalQuantity => Lines.Sum(l => l.Quantity);
    public decimal Discount => TotalQuantity >= MinimumQuantityForDiscount
        ? Math.Round(Subtotal * InstitutionalDiscountRate, 2, MidpointRounding.AwayFromZero)
        : 0m;
    public decimal Total => Subtotal - Discount;
}
