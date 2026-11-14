namespace Bookstore.Ordering;

// Deterministic sample data: the same seed always produces the same orders.
public static class SampleOrders
{
    private static readonly (string Isbn, string Title, decimal Price)[] Books =
    [
        ("9780441172719", "Dune", 349.90m),
        ("9780141439587", "Emma", 189.50m),
        ("9780547928227", "The Hobbit", 259.00m),
        ("9780141439549", "Middlemarch", 219.90m),
        ("9780141439471", "Frankenstein", 149.90m),
        ("9780141439686", "Persuasion", 169.00m),
        ("9780140449136", "Crime and Punishment", 279.50m),
        ("9780679783268", "Pride and Prejudice", 159.90m)
    ];

    private static readonly string[] Customers = ["Ada", "Alan", "Grace", "Linus", "Barbara", "Edsger"];

    public static IEnumerable<Order> Generate(int count, int seed = 42)
    {
        var random = new Random(seed);
        var start = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

        for (int i = 1; i <= count; i++)
        {
            int lineCount = random.Next(1, 4);
            var items = new List<OrderItem>(lineCount);
            for (int l = 0; l < lineCount; l++)
            {
                var book = Books[random.Next(Books.Length)];
                items.Add(new OrderItem(book.Isbn, book.Title, random.Next(1, 4), book.Price));
            }

            yield return new Order(
                OrderNumber: $"BS-{i:D7}",
                CustomerName: Customers[random.Next(Customers.Length)],
                CreatedAtUtc: start.AddSeconds(i * 7),
                Status: (OrderStatus)random.Next(0, 5),
                Currency: "TRY",
                Items: items);
        }
    }
}
