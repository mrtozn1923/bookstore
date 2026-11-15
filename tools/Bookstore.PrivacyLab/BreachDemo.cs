using Bookstore.Data;
using Bookstore.Privacy.Breach;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.PrivacyLab;

public static class BreachDemo
{
    public static async Task RunAsync()
    {
        File.Delete("breach-demo.db");
        await using var db = new BookstoreContext(new DbContextOptionsBuilder<BookstoreContext>()
            .UseSqlite("Data Source=breach-demo.db").Options);
        await db.Database.EnsureCreatedAsync();

        for (int i = 0; i < FakePeople.All.Length; i++)
        {
            db.Customers.Add(FakePeople.ToCustomer(i, new DateTime(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc)));
        }
        // A normal support day, then a stolen support account exporting customers at night.
        var night = new DateTime(2026, 11, 14, 1, 10, 0, DateTimeKind.Utc);
        db.PersonalDataAccesses.AddRange(
            Access("staff-02", 2, "customer-profile", "view", new DateTime(2026, 11, 13, 14, 0, 0, DateTimeKind.Utc)),
            Access("staff-17", 1, "customer-profile", "export", night),
            Access("staff-17", 3, "customer-profile", "export", night.AddMinutes(4)),
            Access("staff-17", 3, "order-billing", "view", night.AddMinutes(5)),
            Access("staff-17", 4, "customer-profile", "export", night.AddMinutes(9)),
            Access("staff-17", 4, "order-billing", "view", night.AddMinutes(30)));
        await db.SaveChangesAsync();

        DateTime detected = new(2026, 11, 14, 9, 30, 0, DateTimeKind.Utc);
        DateTime from = new(2026, 11, 14, 0, 0, 0, DateTimeKind.Utc), to = new(2026, 11, 14, 6, 0, 0, DateTimeKind.Utc);
        BreachAssessment a = await new BreachScope(db).AssessAsync("staff-17", from, to, detected);

        Console.WriteLine($"detected:           {a.DetectedAtUtc:yyyy-MM-dd HH:mm} UTC");
        Console.WriteLine($"notify Authority by {a.AuthorityDeadlineUtc:yyyy-MM-dd HH:mm} UTC");
        Console.WriteLine($"affected customers: {a.Affected.Count}");
        foreach (AffectedCustomer c in a.Affected)
        {
            Console.WriteLine($"  customer {c.CustomerId}: {string.Join(", ", c.Categories)}");
        }
        int reachable = await db.Customers.CountAsync(c => a.Affected.Select(x => x.CustomerId).Contains(c.Id));
        Console.WriteLine($"reachable by e-mail: {reachable}");
        Console.WriteLine();
        Console.Write(BreachNotice.Draft(a, night, night.AddMinutes(30), "kvkk@example.com"));
    }

    private static PersonalDataAccess Access(string actor, int customerId, string category, string action, DateTime at) =>
        new() { ActorId = actor, CustomerId = customerId, DataCategory = category, Action = action, AtUtc = at };
}
