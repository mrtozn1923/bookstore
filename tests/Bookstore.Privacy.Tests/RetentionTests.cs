using Bookstore.Data;
using Bookstore.Ordering;
using Bookstore.Privacy.Retention;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Privacy.Tests;

public sealed class RetentionTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 15, 2, 0, 0, DateTimeKind.Utc);
    private readonly string _ledgerPath = Path.Combine(Path.GetTempPath(), $"ledger-{Guid.NewGuid():N}.txt");

    public void Dispose() => File.Delete(_ledgerPath);

    private static async Task<int> SeedCustomerWithOrderAsync(BookstoreContext db, int index = 3)
    {
        Customer c = FakePeople.ToCustomer(index, Now.AddYears(-1));
        c.Orders.Add(new Order { OrderedAtUtc = Now.AddDays(-10), RetainUntilUtc = RetentionPolicy.OrderBillingRetainUntil(Now.AddDays(-10)), Total = 10m, ShippingAddress = "Example St. 1", BillingName = c.FullName });
        c.Sessions.Add(new CustomerSession { SignedInAtUtc = Now, IpAddress = "127.0.0.1", DeviceId = "device-0001", UserAgent = "test" });
        db.Customers.Add(c);
        await db.SaveChangesAsync();
        return c.Id;
    }

    [Fact]
    public void Order_billing_is_kept_ten_years_after_the_end_of_the_order_year()
    {
        DateTime until = RetentionPolicy.OrderBillingRetainUntil(new DateTime(2014, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), until);
    }

    [Fact]
    public async Task Plain_delete_fails_while_orders_reference_the_customer()
    {
        using var testDb = new TestDb();
        using BookstoreContext db = testDb.CreateContext();
        int id = await SeedCustomerWithOrderAsync(db);
        db.ChangeTracker.Clear();

        db.Customers.Remove(new Customer { Id = id, FullName = "", Email = "", City = "" });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Erasure_keeps_orders_without_the_person_and_logs_it()
    {
        using var testDb = new TestDb();
        using BookstoreContext db = testDb.CreateContext();
        int id = await SeedCustomerWithOrderAsync(db);
        var processor = new ErasureProcessor(db, [], new FileErasureLedger(_ledgerPath), new FixedClock(Now));

        ErasureResult result = await processor.EraseCustomerAsync(id);

        Assert.Equal(1, result.OrdersDetached);
        Assert.False(await db.Customers.AnyAsync(c => c.Id == id));
        Assert.False(await db.CustomerSessions.AnyAsync(s => s.CustomerId == id));
        Assert.Equal(1, await db.Orders.CountAsync(o => o.CustomerId == null));
        Assert.Single(await db.DestructionLog.ToListAsync());
    }

    [Fact]
    public async Task Ledger_replay_erases_a_customer_restored_from_backup()
    {
        using var testDb = new TestDb();
        using BookstoreContext db = testDb.CreateContext();
        int id = await SeedCustomerWithOrderAsync(db);
        var processor = new ErasureProcessor(db, [], new FileErasureLedger(_ledgerPath), new FixedClock(Now));
        await processor.EraseCustomerAsync(id);

        // Simulate a restore: the same customer row is back.
        Customer restored = FakePeople.ToCustomer(3, Now);
        restored.Id = id;
        db.Customers.Add(restored);
        await db.SaveChangesAsync();

        int replayed = await processor.ReapplyLedgerAsync();

        Assert.Equal(1, replayed);
        Assert.False(await db.Customers.AnyAsync(c => c.Id == id));
    }
}
