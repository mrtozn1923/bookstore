using Bookstore.Data;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Ordering;

// Development only: fills an empty Orders table with deterministic demo orders for the admin screens.
internal static class DemoOrders
{
    private static readonly string[] FirstNames =
        ["Ada", "Alan", "Grace", "Linus", "Margaret", "Dennis", "Barbara", "Edsger", "Frances", "Donald", "Katherine", "Niklaus"];
    private static readonly string[] LastNames =
        ["Lovelace", "Turing", "Hopper", "Torvalds", "Hamilton", "Ritchie", "Liskov", "Dijkstra", "Allen", "Knuth", "Johnson", "Wirth",
         "Kay", "Lamport", "Thompson", "Backus", "Codd", "Floyd", "Hoare", "McCarthy"];

    // Weighted so that the list looks like a real shop: most orders are delivered, some are cancelled.
    private static readonly (OrderStatus Status, int Weight)[] Statuses =
    [
        (OrderStatus.Delivered, 50), (OrderStatus.Shipped, 15), (OrderStatus.Paid, 10),
        (OrderStatus.PaymentPending, 5), (OrderStatus.Placed, 8), (OrderStatus.Cancelled, 12)
    ];

    public static async Task SeedAsync(BookstoreContext db, int count, TimeProvider clock, CancellationToken ct = default)
    {
        if (count <= 0 || await db.Orders.AnyAsync(ct))
        {
            return;
        }

        Random random = new(2026);
        var books = await db.Books.AsNoTracking().OrderBy(b => b.Id).Select(b => new { b.Id, b.Title, b.Price }).ToListAsync(ct);

        List<Customer> customers =
        [
            .. LastNames.SelectMany(last => FirstNames.Select(first => new Customer
            {
                Name = $"{first} {last}",
                Email = $"{first}.{last}@example.com".ToLowerInvariant()
            }))
        ];
        db.Customers.AddRange(customers);
        await db.SaveChangesAsync(ct);
        int[] customerIds = [.. customers.Select(c => c.Id)];
        db.ChangeTracker.Clear();

        DateTime now = clock.GetUtcNow().UtcDateTime;
        int totalWeight = Statuses.Sum(s => s.Weight);
        for (int created = 0; created < count; created += 1000)
        {
            for (int i = created; i < Math.Min(count, created + 1000); i++)
            {
                Order order = new()
                {
                    CustomerId = customerIds[random.Next(customerIds.Length)],
                    PlacedAtUtc = now.AddMinutes(-random.Next(60, 365 * 24 * 60)),
                    Status = PickStatus(random.Next(totalWeight))
                };
                foreach (var book in books.OrderBy(_ => random.Next()).Take(random.Next(1, 4)))
                {
                    int quantity = random.Next(1, 4);
                    order.Items.Add(new OrderItem
                    {
                        BookId = book.Id,
                        Title = book.Title,
                        UnitPrice = book.Price,
                        Quantity = quantity,
                        LineTotal = OrderItem.CalculateLineTotal(book.Price, quantity)
                    });
                }
                order.Total = order.Items.Sum(item => item.LineTotal);
                db.Orders.Add(order);
            }
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
        }
    }

    private static OrderStatus PickStatus(int roll)
    {
        foreach ((OrderStatus status, int weight) in Statuses)
        {
            if (roll < weight)
            {
                return status;
            }
            roll -= weight;
        }
        return OrderStatus.Delivered;
    }
}
