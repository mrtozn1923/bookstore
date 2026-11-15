#:project ../src/Bookstore.Ordering/Bookstore.Ordering.csproj
using Bookstore.Catalog;
using Bookstore.Ordering;

Order order = new();
order.AddItem(new OrderItem(Isbn.Parse("9780261103344"), "The Hobbit", 149.90m, 2));

InvoiceData data = InvoiceTransform.Enrich(order, discountRate: 0.10m, vatRate: 0.10m);
InvoiceCalculator calculator = new(order, discountRate: 0.10m, vatRate: 0.10m);

order.AddItem(new OrderItem(Isbn.Parse("9780441013593"), "Dune", 12.45m, 1));

Console.WriteLine($"transform : {data.Total}");
Console.WriteLine($"calculator: {calculator.Total}");

InvoiceData noDiscount = data with { Discount = 0m };
Console.WriteLine($"with      : discount {noDiscount.Discount}, total {noDiscount.Total}");

public sealed record InvoiceData(decimal BaseAmount, decimal Discount, decimal Vat, decimal Total);

public static class InvoiceTransform
{
    public static InvoiceData Enrich(Order order, decimal discountRate, decimal vatRate)
    {
        decimal baseAmount = order.Total;
        decimal discount = Math.Round(baseAmount * discountRate, 2, MidpointRounding.AwayFromZero);
        decimal vat = Math.Round((baseAmount - discount) * vatRate, 2, MidpointRounding.AwayFromZero);
        return new InvoiceData(baseAmount, discount, vat, baseAmount - discount + vat);
    }
}
