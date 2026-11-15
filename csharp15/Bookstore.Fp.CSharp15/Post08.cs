using System.Text.Json;
using Closed = Bookstore.Fp.CSharp15.Closed;

namespace Bookstore.Fp.CSharp15;

public static class Post08
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    public static void ClosedHierarchy()
    {
        Closed.OrderStatus[] history = [new Closed.Pending(), new Closed.Paid("PAY-1001"), new Closed.Shipped("PAY-1001", "TRK-778"), new Closed.Cancelled("customer request")];
        foreach (Closed.OrderStatus status in history)
            Console.WriteLine(Closed.OrderWorkflow.Describe(status));
    }

    public static void Results()
    {
        Dictionary<string, int> stock = new() { ["9780441013593"] = 1 };

        foreach ((string isbn, int quantity) in new[] { ("9780441013593", 1), ("9780441013593", 2), ("9780261103344", 1) })
        {
            Result<string> result = FindStock(stock, isbn)
                .Bind<int>(left => left >= quantity ? new Ok<int>(left - quantity) : new Failed([new StockInsufficient(isbn, left)]))
                .Map(after => $"{quantity} x {isbn} reserved, {after} left");

            Console.WriteLine(result switch
            {
                Ok<string>(var text) => $"ok: {text}",
                Failed(var errors) => $"failed: {string.Join(", ", errors.Select(e => $"{OrderErrors.StatusCode(e)} {OrderErrors.Message(e)}"))}",
                null => "default"
            });
        }

        Result<int> empty = default;
        Console.WriteLine($"default: {empty.Value is null}");
    }

    private static Result<int> FindStock(Dictionary<string, int> stock, string isbn) =>
        stock.TryGetValue(isbn, out int left) ? new Ok<int>(left) : new Failed([new BookNotFound(isbn)]);

    public static void Json()
    {
        OrderStatus[] statuses = [new Pending(), new Paid("PAY-1001"), new Cancelled("customer request")];
        string unionJson = JsonSerializer.Serialize(statuses, Web);
        Console.WriteLine($"union:  {unionJson}");
        try
        {
            OrderStatus[] back = JsonSerializer.Deserialize<OrderStatus[]>(unionJson, Web)!;
            Console.WriteLine($"union back: {string.Join(", ", back.Select(s => s.Value?.GetType().Name ?? "null"))}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"union back: {ex.GetType().Name}: {ex.Message}");
        }

        Closed.OrderStatus[] closed = [new Closed.Pending(), new Closed.Paid("PAY-1001"), new Closed.Cancelled("customer request")];
        Console.WriteLine($"closed: {JsonSerializer.Serialize(closed, Web)}");

        JsonSerializerOptions inferred = new(Web) { InferClosedTypePolymorphism = true };
        string closedJson = JsonSerializer.Serialize(closed, inferred);
        Console.WriteLine($"closed + infer: {closedJson}");
        Closed.OrderStatus[] closedBack = JsonSerializer.Deserialize<Closed.OrderStatus[]>(closedJson, inferred)!;
        Console.WriteLine($"closed back: {closedBack.SequenceEqual(closed)}");
    }
}
