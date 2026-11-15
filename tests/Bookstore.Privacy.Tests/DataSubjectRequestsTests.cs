using Bookstore.Data;
using Bookstore.Ordering;

namespace Bookstore.Privacy.Tests;

public sealed class DataSubjectRequestsTests
{
    private static readonly DateTime Now = new(2026, 11, 15, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Export_includes_ip_address_and_device_id()
    {
        using var testDb = new TestDb();
        int id;
        using (BookstoreContext db = testDb.CreateContext())
        {
            Customer c = FakePeople.ToCustomer(0, Now);
            c.Sessions.Add(new CustomerSession { SignedInAtUtc = Now, IpAddress = "127.0.0.1", DeviceId = "device-0001", UserAgent = "test" });
            db.Customers.Add(c);
            await db.SaveChangesAsync();
            id = c.Id;
        }

        using BookstoreContext read = testDb.CreateContext();
        PersonalDataExport? export = await new DataSubjectRequests(read, new FixedClock(Now)).ExportAsync(id);

        SessionExport session = Assert.Single(export!.Sessions);
        Assert.Equal("127.0.0.1", session.IpAddress);
        Assert.Equal("device-0001", session.DeviceId);
    }

    [Fact]
    public async Task Erasure_request_is_due_in_30_days()
    {
        using var testDb = new TestDb();
        using BookstoreContext db = testDb.CreateContext();
        Customer c = FakePeople.ToCustomer(1, Now);
        db.Customers.Add(c);
        await db.SaveChangesAsync();

        ErasureRequest request = await new DataSubjectRequests(db, new FixedClock(Now)).RequestErasureAsync(c.Id);

        Assert.Equal(new DateTime(2026, 12, 15, 9, 0, 0, DateTimeKind.Utc), request.DueAtUtc);
    }
}
