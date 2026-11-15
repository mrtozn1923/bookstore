using System.Text.RegularExpressions;
using Bookstore.Ordering.Functional;
using Bookstore.Ordering.Orders;
using Bookstore.Ordering.Pricing;
using static Bookstore.Ordering.Orders.OrderStatus;

namespace Bookstore.Fp.Demo;

public static class Post07
{
    private static readonly TimeSpan Istanbul = TimeSpan.FromHours(3);

    private static readonly PlaceOrderRequest Broken = new("ada.example.com", "9780441013599", 0, null);

    public static void Guess()
    {
        Result<ValidOrderRequest> result = OrderValidation.ValidateFirstError(Broken);
        Console.WriteLine(result);
        Console.WriteLine($"errors shown to the customer: {result.Errors.Count}");
    }

    public static void Combine()
    {
        Result<ValidOrderRequest> result = OrderValidation.Validate(Broken);
        Console.WriteLine(result);
        foreach (Error error in result.Errors)
            Console.WriteLine($"  {error.Code}: {error.Message}");

        Console.WriteLine(OrderValidation.Validate(new("ada@example.com", "978-0-441-01359-3", 2, null)));
    }

    public static OrderPlacement CreatePlacement(TextWriter log)
    {
        Dictionary<string, CatalogBook> catalog = new()
        {
            ["9780441013593"] = new("9780441013593", "Dune", 189.50m, IsEbook: false),
            ["9780141439587"] = new("9780141439587", "Emma", 59.90m, IsEbook: true)
        };
        InMemoryStock stock = new(new() { ["9780441013593"] = 3, ["9780141439587"] = 100 });
        Campaign autumn = new("AUTUMN10", 0.10m, new(2026, 11, 1, 0, 0, 0, Istanbul), new(2026, 12, 1, 0, 0, 0, Istanbul));
        FixedClock clock = new(new DateTimeOffset(2026, 11, 15, 9, 0, 0, TimeSpan.Zero));
        int next = 41;
        return new OrderPlacement(catalog, stock, autumn, clock, () => $"BS-2026-{++next:D4}", log);
    }

    public static async Task Place()
    {
        OrderPlacement placement = CreatePlacement(Console.Out);
        PlaceOrderRequest[] requests =
        [
            new("ada@example.com", "9780441013593", 2, "autumn10"),
            new("alan@example.com", "9780261103344", 1, null),
            new("grace@example.com", "9780441013593", 2, null),
            Broken
        ];

        foreach (PlaceOrderRequest request in requests)
        {
            Result<PlacedOrder> result = await placement.PlaceAsync(request);
            Console.WriteLine(result.Match(
                order => $"placed {order.OrderNo}: {order.Totals.Total} TL",
                errors => $"rejected: {string.Join(", ", errors.Select(error => error.Code))}"));
        }
    }

    public static void Cancel()
    {
        OrderStatus[] statuses = [new Pending(), new Paid("PAY-1001"), new Shipped("PAY-1001", "TRK-778"), new Cancelled("customer request")];
        foreach (OrderStatus status in statuses)
        {
            Result<Cancelled> result = OrderCancellation.TryCancel(status, "customer request");
            Console.WriteLine($"{status.GetType().Name,-9} -> {result.Match(c => c.ToString(), errors => errors[0].Message)}");
        }
    }

    public static void StackTrace()
    {
        Result<decimal> wrapped = TryParsePrice("12,50 TL");
        Console.WriteLine($"as Result: {wrapped.Match(p => p.ToString(), errors => errors[0].Message)}");

        try
        {
            PriceText.Parse("12,50 TL");
        }
        catch (FormatException ex)
        {
            // Strip file paths so the output is the same on every machine
            string[] frames = ex.StackTrace!.Split(Environment.NewLine).Select(f => Regex.Replace(f, @" in .*$", "")).ToArray();
            Console.WriteLine($"as exception: {frames.Length} frames, ours: {frames.First(f => f.Contains("PriceText")).Trim()}");
        }
    }

    // Anti-pattern: the exception becomes a message and its stack trace is lost
    private static Result<decimal> TryParsePrice(string text)
    {
        try
        {
            return PriceText.Parse(text);
        }
        catch (FormatException ex)
        {
            return new Error("price.format", ex.Message);
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
