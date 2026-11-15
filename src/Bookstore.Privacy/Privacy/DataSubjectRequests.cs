using Bookstore.Data;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Privacy;

public sealed record PersonalDataExport(
    ProfileExport Profile,
    IReadOnlyList<OrderExport> Orders,
    IReadOnlyList<SessionExport> Sessions,
    DateTime ExportedAtUtc);

public sealed record ProfileExport(string FullName, string Email, string? Phone, DateOnly? BirthDate, string City);
public sealed record OrderExport(int OrderId, DateTime OrderedAtUtc, decimal Total, string ShippingAddress);
public sealed record SessionExport(DateTime SignedInAtUtc, string IpAddress, string DeviceId, string UserAgent);
public sealed record ProfileCorrection(string? FullName, string? Phone, string? City);

public sealed class DataSubjectRequests(BookstoreContext db, TimeProvider clock)
{
    // KVKK md. 13/2: requests are answered within 30 days at the latest.
    public static readonly TimeSpan ResponseDeadline = TimeSpan.FromDays(30);

    public async Task<PersonalDataExport?> ExportAsync(int customerId, CancellationToken ct = default)
    {
        var customer = await db.Customers.AsNoTracking()
            .Where(c => c.Id == customerId)
            .Select(c => new ProfileExport(c.FullName, c.Email, c.Phone, c.BirthDate, c.City))
            .SingleOrDefaultAsync(ct);
        if (customer is null)
        {
            return null;
        }

        List<OrderExport> orders = await db.Orders.AsNoTracking()
            .Where(o => o.CustomerId == customerId)
            .OrderBy(o => o.OrderedAtUtc)
            .Select(o => new OrderExport(o.Id, o.OrderedAtUtc, o.Total, o.ShippingAddress))
            .ToListAsync(ct);

        List<SessionExport> sessions = await db.CustomerSessions.AsNoTracking()
            .Where(s => s.CustomerId == customerId)
            .OrderBy(s => s.SignedInAtUtc)
            .Select(s => new SessionExport(s.SignedInAtUtc, s.IpAddress, s.DeviceId, s.UserAgent))
            .ToListAsync(ct);

        return new PersonalDataExport(customer, orders, sessions, clock.GetUtcNow().UtcDateTime);
    }

    public async Task<bool> CorrectAsync(int customerId, ProfileCorrection change, CancellationToken ct = default)
    {
        var customer = await db.Customers.SingleOrDefaultAsync(c => c.Id == customerId, ct);
        if (customer is null)
        {
            return false;
        }

        customer.FullName = change.FullName ?? customer.FullName;
        customer.Phone = change.Phone ?? customer.Phone;
        customer.City = change.City ?? customer.City;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ErasureRequest> RequestErasureAsync(int customerId, CancellationToken ct = default)
    {
        DateTime now = clock.GetUtcNow().UtcDateTime;
        var request = new ErasureRequest
        {
            CustomerId = customerId,
            RequestedAtUtc = now,
            DueAtUtc = now + ResponseDeadline,
        };
        db.ErasureRequests.Add(request);
        await db.SaveChangesAsync(ct);
        return request;
    }
}
