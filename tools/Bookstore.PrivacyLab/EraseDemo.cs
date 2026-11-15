using Bookstore.Data;
using Bookstore.Ordering;
using Bookstore.Privacy;
using Bookstore.Privacy.Retention;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.PrivacyLab;

// Stand-ins for the copies outside the database (search index, Redis cache).
public sealed class InMemoryCopy(string name, HashSet<int> customerIds) : IPersonalDataCopy
{
    public string Name => name;

    public Task EraseCustomerAsync(int customerId, CancellationToken ct)
    {
        customerIds.Remove(customerId);
        return Task.CompletedTask;
    }
}

public sealed class DemoClock(DateTime utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
}

public static class EraseDemo
{
    private const string Live = "erase-demo.db";
    private const string Backup = "erase-demo-backup.db";
    private const string Ledger = "erasure-ledger.txt";

    public static async Task RunAsync()
    {
        foreach (string f in new[] { Live, Backup, Ledger }) File.Delete(f);
        var clock = new DemoClock(new DateTime(2026, 11, 15, 2, 0, 0, DateTimeKind.Utc));
        await SeedAsync(clock);

        // Nightly backup, taken before the erasure request arrives.
        await using (BookstoreContext db = Open())
        {
            await db.Database.ExecuteSqlRawAsync($"VACUUM INTO '{Backup}'");
        }

        // Pitfall: a plain delete hits the orders' foreign key.
        await using (BookstoreContext db = Open())
        {
            Customer linus = await db.Customers.SingleAsync(c => c.FullName == "Linus Fakely");
            db.Customers.Remove(linus);
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"plain delete: {ex.InnerException?.Message}");
            }
        }

        HashSet<int> searchIndex = [1, 2, 3, 4, 5, 6], cache = [4];
        IPersonalDataCopy[] copies = [new InMemoryCopy("search-index", searchIndex), new InMemoryCopy("redis-cache", cache)];
        var ledger = new FileErasureLedger(Ledger);

        await using (BookstoreContext db = Open())
        {
            db.ErasureRequests.Add(new ErasureRequest { CustomerId = 4, RequestedAtUtc = clock.GetUtcNow().UtcDateTime.AddDays(-2), DueAtUtc = clock.GetUtcNow().UtcDateTime.AddDays(28) });
            await db.SaveChangesAsync();

            var job = new PeriodicDestructionJob(db, new ErasureProcessor(db, copies, ledger, clock), clock);
            DestructionRun run = await job.RunAsync();
            Console.WriteLine($"run: {run}");
            Console.WriteLine($"customers={await db.Customers.CountAsync()} orders={await db.Orders.CountAsync()} orphan orders={await db.Orders.CountAsync(o => o.CustomerId == null)} search-index=[{string.Join(",", searchIndex)}]");
            foreach (DestructionLogEntry e in await db.DestructionLog.OrderBy(e => e.Id).ToListAsync())
            {
                Console.WriteLine($"log: {e.Category,-16} {e.Method,-9} {e.RecordCount} ({e.Reason})");
            }
        }

        // Disaster: the live database is restored from last night's backup.
        File.Copy(Backup, Live, overwrite: true);
        await using (BookstoreContext db = Open())
        {
            Console.WriteLine($"after restore: Linus present = {await db.Customers.AnyAsync(c => c.Id == 4)}");
            int replayed = await new ErasureProcessor(db, copies, ledger, clock).ReapplyLedgerAsync();
            Console.WriteLine($"ledger replay: {replayed} customer(s) erased again, Linus present = {await db.Customers.AnyAsync(c => c.Id == 4)}");
        }
    }

    private static BookstoreContext Open() =>
        new(new DbContextOptionsBuilder<BookstoreContext>().UseSqlite($"Data Source={Live};Pooling=False").Options);

    private static async Task SeedAsync(TimeProvider clock)
    {
        await using BookstoreContext db = Open();
        await db.Database.EnsureCreatedAsync();
        DateTime now = clock.GetUtcNow().UtcDateTime;
        for (int i = 0; i < FakePeople.All.Length; i++)
        {
            Customer c = FakePeople.ToCustomer(i, now.AddYears(-12));
            // One order from 2014 (past its retention period) and one from this year.
            foreach (DateTime orderedAt in new[] { new DateTime(2014, 6, 1, 0, 0, 0, DateTimeKind.Utc), now.AddDays(-30) })
            {
                c.Orders.Add(new Order
                {
                    OrderedAtUtc = orderedAt,
                    RetainUntilUtc = RetentionPolicy.OrderBillingRetainUntil(orderedAt),
                    Total = 180.00m,
                    ShippingAddress = $"Example St. {i + 1}, {c.City}",
                    BillingName = c.FullName,
                });
            }
            c.Sessions.Add(new CustomerSession { SignedInAtUtc = now.AddDays(-200), IpAddress = "127.0.0.1", DeviceId = $"device-{i + 1:0000}", UserAgent = "test" });
            c.Sessions.Add(new CustomerSession { SignedInAtUtc = now.AddDays(-3), IpAddress = "127.0.0.1", DeviceId = $"device-{i + 1:0000}", UserAgent = "test" });
            db.Customers.Add(c);
        }
        await db.SaveChangesAsync();
    }
}
