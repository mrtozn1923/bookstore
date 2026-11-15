#:property PublishAot=false
using System.Text.Json;

Order[] orders =
[
    new("BS-1001", "Ada", new DateTime(2026, 11, 14, 9, 30, 0, DateTimeKind.Utc), 2, 49.80m, "Paid"),
    new("BS-1002", "Alan", new DateTime(2026, 11, 14, 11, 5, 0, DateTimeKind.Utc), 1, 24.90m, "Shipped"),
    new("BS-1003", "Grace", new DateTime(2026, 11, 15, 8, 0, 0, DateTimeKind.Utc), 25, 1120.50m, "AwaitingApproval"),
];

// 1) a single JSON array: the whole file is one value
JsonSerializerOptions indented = new(JsonSerializerOptions.Web) { WriteIndented = true };
File.WriteAllText("orders.json", JsonSerializer.Serialize(orders, indented));

// 2) JSON Lines: one compact value per line, each line ends with \n
await using (StreamWriter writer = new("orders.jsonl"))
{
    foreach (Order order in orders)
    {
        await writer.WriteAsync(JsonSerializer.Serialize(order, JsonSerializerOptions.Web));
        await writer.WriteAsync('\n');
    }
}

// read back line by line without loading the whole file
await using FileStream stream = File.OpenRead("orders.jsonl");
await foreach (Order? order in JsonSerializer.DeserializeAsyncEnumerable<Order>(
    stream, topLevelValues: true, JsonSerializerOptions.Web))
{
    Console.WriteLine($"{order!.OrderNumber} {order.Status} {order.Total}");
}

record Order(string OrderNumber, string CustomerName, DateTime CreatedAt, int Quantity, decimal Total, string Status);
