namespace Bookstore.Notifications.Documents;

public sealed record PostalAddress(string Name, string Line1, string City, string PostalCode, string Country);

public sealed record InvoiceLine(string Isbn, string Title, int Quantity, decimal UnitPrice, decimal VatRate)
{
    public decimal NetAmount => Quantity * UnitPrice;
    public decimal VatAmount => Math.Round(NetAmount * VatRate, 2, MidpointRounding.AwayFromZero);
}

public sealed record InvoiceModel(
    string InvoiceNumber,
    int OrderId,
    DateOnly IssueDate,
    string CustomerEmail,
    PostalAddress BillingAddress,
    IReadOnlyList<InvoiceLine> Lines,
    string Currency = "TRY")
{
    public decimal NetTotal => Lines.Sum(l => l.NetAmount);
    public decimal VatTotal => Lines.Sum(l => l.VatAmount);
    public decimal GrandTotal => NetTotal + VatTotal;
}
