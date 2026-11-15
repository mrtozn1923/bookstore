using Microsoft.Extensions.Time.Testing;

namespace Bookstore.Ordering.Tests;

public sealed class InstitutionalFulfillmentTests
{
    private const string Dune = "9780441013593";
    private const string Emma = "9780141439518";
    private static readonly Dictionary<string, string> Isbns = new() { ["Dune"] = Dune, ["Emma"] = Emma, ["Hobbit"] = "9780547928227" };
    private static readonly DateTimeOffset OrderedAt = new(2026, 9, 1, 9, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _clock = new(OrderedAt);

    // Nevin Hanım's school: 12 Dune (150 TRY) and 10 Emma (95 TRY), 22 books in total; shipping 49.90 TRY
    private InstitutionalFulfillment NewOrder() =>
        new([new(Dune, 12, 150m), new(Emma, 10, 95m)], 49.90m, OrderedAt, _clock);

    // "Dune:12 Emma:10" -> { Dune ISBN: 12, Emma ISBN: 10 }
    private static Dictionary<string, int> Items(string shipment) =>
        shipment.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Split(':'))
            .ToDictionary(parts => Isbns[parts[0]], parts => int.Parse(parts[1]));

    // T1-T6: decision table (shipment number x completes the order) and state transitions together
    public static IEnumerable<TheoryDataRow<string[], FulfillmentState, decimal[]>> ShipmentRules =>
    [
        new(["Dune:12 Emma:10"], FulfillmentState.Shipped, [49.90m]) { Label = "T1 first shipment completes" },
        new(["Dune:12"], FulfillmentState.PartiallyShipped, [49.90m]) { Label = "T2 first shipment is partial" },
        new(["Dune:1"], FulfillmentState.PartiallyShipped, [49.90m]) { Label = "T3 one book (lower boundary)" },
        new(["Dune:12", "Emma:10"], FulfillmentState.Shipped, [49.90m, 0m]) { Label = "T4 second completes (exact remainder)" },
        new(["Dune:6", "Emma:10"], FulfillmentState.PartiallyShipped, [49.90m, 0m]) { Label = "T5 second is partial" },
        new(["Dune:6", "Dune:3", "Dune:3 Emma:10"], FulfillmentState.Shipped, [49.90m, 0m, 0m]) { Label = "T6 third completes" },
    ];

    [Theory]
    [MemberData(nameof(ShipmentRules))]
    public void Shipments_move_the_order_and_only_the_first_one_is_charged(
        string[] shipments, FulfillmentState expectedState, decimal[] expectedFees)
    {
        InstitutionalFulfillment order = NewOrder();

        foreach (string shipment in shipments)
        {
            order.Ship(Items(shipment));
        }

        Assert.Equal(expectedState, order.State);
        Assert.Equal(expectedFees, order.Shipments.Select(s => s.ShippingFee));
    }

    // T7: the decision table's only "no" rule
    [Fact]
    public void Third_shipment_that_does_not_complete_the_order_is_rejected()
    {
        InstitutionalFulfillment order = NewOrder();
        order.Ship(Items("Dune:6"));
        order.Ship(Items("Dune:3"));

        Assert.Throws<InvalidOperationException>(() => order.Ship(Items("Dune:3")));
        Assert.Equal(FulfillmentState.PartiallyShipped, order.State);
        Assert.Equal(2, order.Shipments.Count);
    }

    // T8-T12: equivalence partitioning and boundary value analysis; one invalid value per test
    [Theory]
    [InlineData("Dune:0")]
    [InlineData("Dune:13")]
    [InlineData("Emma:11")]
    [InlineData("Hobbit:1")]
    [InlineData("")]
    public void Invalid_shipment_is_rejected_and_nothing_is_shipped(string shipment)
    {
        InstitutionalFulfillment order = NewOrder();

        Assert.Throws<ArgumentException>(() => order.Ship(Items(shipment)));
        Assert.Equal(FulfillmentState.Paid, order.State);
        Assert.Empty(order.Shipments);
    }

    // T13: the invoice covers only the shipped books
    [Fact]
    public void Invoice_covers_only_the_shipped_books()
    {
        InstitutionalFulfillment order = NewOrder();

        Shipment first = order.Ship(Items("Dune:12"));
        Shipment second = order.Ship(Items("Emma:10"));

        Assert.Equal(1800m, first.InvoiceAmount);
        Assert.Equal(950m, second.InvoiceAmount);
    }

    // T14-T15: guard [at least 30 days after the order date]; the boundary and just before it
    [Fact]
    public void Remaining_books_cannot_be_cancelled_before_30_days()
    {
        InstitutionalFulfillment order = NewOrder();
        order.Ship(Items("Dune:12"));
        _clock.Advance(InstitutionalFulfillment.BackorderWindow - TimeSpan.FromTicks(1));

        Assert.Throws<InvalidOperationException>(() => order.CancelRemaining());
        Assert.Equal(FulfillmentState.PartiallyShipped, order.State);
    }

    [Fact]
    public void Remaining_books_are_cancelled_and_refunded_after_30_days()
    {
        InstitutionalFulfillment order = NewOrder();
        order.Ship(Items("Dune:12"));
        _clock.Advance(InstitutionalFulfillment.BackorderWindow);

        decimal refund = order.CancelRemaining();

        Assert.Equal(950m, refund);
        Assert.Equal(FulfillmentState.ClosedWithCancellation, order.State);
    }

    // T16-T19: invalid transitions in final states
    [Fact]
    public void Shipped_order_cannot_ship_or_cancel_again()
    {
        InstitutionalFulfillment order = NewOrder();
        order.Ship(Items("Dune:12 Emma:10"));
        _clock.Advance(InstitutionalFulfillment.BackorderWindow);

        Assert.Throws<InvalidOperationException>(() => order.Ship(Items("Dune:1")));
        Assert.Throws<InvalidOperationException>(() => order.CancelRemaining());
    }

    [Fact]
    public void Closed_order_cannot_ship_or_cancel_again()
    {
        InstitutionalFulfillment order = NewOrder();
        _clock.Advance(InstitutionalFulfillment.BackorderWindow);
        order.CancelRemaining();

        Assert.Throws<InvalidOperationException>(() => order.Ship(Items("Dune:1")));
        Assert.Throws<InvalidOperationException>(() => order.CancelRemaining());
    }
}
