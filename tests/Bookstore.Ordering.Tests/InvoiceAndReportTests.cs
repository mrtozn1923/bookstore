using Bookstore.Catalog;
using Bookstore.Ordering;

namespace Bookstore.Ordering.Tests;

public class InvoiceAndReportTests
{
    private static Order NewOrder(int customerId, DateOnly placedOn, string city, params (string Isbn, decimal Price, int Qty)[] lines)
    {
        Address address = new("1 Library Lane", city, "00000");
        Order order = new()
        {
            Customer = new Customer { Id = customerId, Name = "Ada", Address = address },
            ShippingAddress = address,
            PlacedOn = placedOn
        };
        foreach ((string isbn, decimal price, int qty) in lines)
            order.AddItem(new OrderItem(Isbn.Parse(isbn), "Book", price, qty));
        return order;
    }

    private static readonly Order[] Orders =
    [
        NewOrder(7, new DateOnly(2026, 10, 30), "Ankara", ("9780441013593", 12.45m, 1)),
        NewOrder(7, new DateOnly(2026, 11, 1), "Ankara", ("9780261103344", 149.90m, 2)),
        NewOrder(8, new DateOnly(2026, 11, 14), "Van", ("9780141439587", 59.90m, 1)),
        NewOrder(9, new DateOnly(2026, 12, 1), "Izmir", ("9780141439549", 129.90m, 3))
    ];

    [Fact]
    public void Invoice_amounts_for_ten_percent_discount_and_vat()
    {
        InvoiceCalculator invoice = new(Orders[1], discountRate: 0.10m, vatRate: 0.10m);

        Assert.Equal(299.80m, invoice.BaseAmount);
        Assert.Equal(29.98m, invoice.Discount);
        Assert.Equal(26.98m, invoice.Vat);
        Assert.Equal(296.80m, invoice.Total);
    }

    [Fact]
    public void Report_for_november()
    {
        DateRange november = new(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30));

        Assert.Equal(359.70m, SalesReport.Revenue(Orders, november));
        Assert.Equal(2, SalesReport.OrderCount(Orders, november));
        Assert.Equal(2, SalesReport.OrdersOfCustomer(Orders, 7));
    }

    [Fact]
    public void Loyalty_line_shows_customer_data() =>
        Assert.Equal("Ada (7): 0 points, Ankara", LoyaltyReport.Line(Orders[0].Customer));

    [Theory]
    [InlineData(0, 49.90)]
    [InlineData(1, 49.90)]
    [InlineData(2, 64.90)]
    [InlineData(3, 0)]
    public void Shipping_fee_by_city_and_total(int orderIndex, decimal expected)
    {
        Order order = Orders[orderIndex];

        Assert.Equal(expected, ShippingQuote.FeeFor(order));
    }
}
