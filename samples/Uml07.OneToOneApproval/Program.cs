using Bookstore.Institutional.OneToOne;
using Microsoft.EntityFrameworkCore;

string connectionString = Environment.GetEnvironmentVariable("BOOKSTORE_DB")
    ?? "Server=localhost;Database=BookstoreDb;Trusted_Connection=True;TrustServerCertificate=True";
List<string> sql = [];
await using OrderContext db = new(connectionString, sql);
await db.Database.EnsureDeletedAsync();
await db.Database.EnsureCreatedAsync();

Order order = new() { Approval = new() { Approver = "Alan Turing", Step = "Approver" } };
db.Orders.Add(order);
await db.SaveChangesAsync();

order.Approval = new() { Approver = "Grace Hopper", Step = "PurchasingManager" };
order.Status = "Approved";
await db.SaveChangesAsync();

// İkinci SaveChangesAsync'in gönderdiği SQL ve tablonun son hâli
Console.WriteLine(sql[^1]);
foreach (PurchaseApproval a in await db.PurchaseApprovals.AsNoTracking().ToListAsync())
    Console.WriteLine($"PurchaseApproval {a.Id}: {a.Step} by {a.Approver}");
