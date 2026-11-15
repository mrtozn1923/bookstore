using System.Text.Json;
using Bookstore.Ordering.Legacy;
using Bookstore.Ordering.Orders;
using static Bookstore.Ordering.Orders.OrderStatus;

namespace Bookstore.Fp.Demo;

public static class Post05
{
    public static void Flags()
    {
        FlaggedOrder order = new() { IsPaid = true, PaymentId = "PAY-1001" };

        // Support cancels the order; the warehouse ships it in the same minute
        order.IsCancelled = true;
        order.CancelReason = "customer request";
        order.IsShipped = true;
        order.TrackingNo = "TRK-778";

        Console.WriteLine(order.Describe());
        Console.WriteLine($"paid: {order.IsPaid}, shipped: {order.IsShipped}, cancelled: {order.IsCancelled}");

        FlaggedOrder ghost = new() { IsShipped = true };
        Console.WriteLine($"ghost: {ghost.Describe()}");
    }

    public static void Workflow()
    {
        Pending pending = new();
        Paid paid = OrderWorkflow.Pay(pending, "PAY-1001");
        Shipped shipped = OrderWorkflow.Ship(paid, "TRK-778");
        Cancelled cancelled = OrderWorkflow.Cancel(paid, "customer request");

        OrderStatus[] history = [pending, paid, shipped, cancelled];
        foreach (OrderStatus status in history)
            Console.WriteLine(OrderWorkflow.Describe(status));

        Order order = new("BS-2026-0042", "ada@example.com", 259.36m, shipped);
        Console.WriteLine(order);
    }

    public static void Json()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        OrderStatus[] statuses = [new Pending(), new Paid("PAY-1001"), new Shipped("PAY-1001", "TRK-778"), new Cancelled("customer request")];

        string json = JsonSerializer.Serialize(statuses, options);
        Console.WriteLine(json);

        OrderStatus[] back = JsonSerializer.Deserialize<OrderStatus[]>(json, options)!;
        Console.WriteLine($"round trip: {back.SequenceEqual(statuses)}, last: {back[^1]}");
    }
}
