using Bookstore.Data;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Privacy.Retention;

public sealed record DestructionRun(int ErasureRequests, int SessionsDeleted, int OrdersAnonymised);

public sealed class PeriodicDestructionJob(BookstoreContext db, ErasureProcessor erasure, TimeProvider clock)
{
    public async Task<DestructionRun> RunAsync(CancellationToken ct = default)
    {
        DateTime now = clock.GetUtcNow().UtcDateTime;

        List<int> pending = await db.ErasureRequests.Where(r => r.CompletedAtUtc == null)
            .Select(r => r.CustomerId).Distinct().ToListAsync(ct);
        foreach (int customerId in pending)
        {
            await erasure.EraseCustomerAsync(customerId, ct);
        }

        DateTime sessionCutoff = now - RetentionPolicy.SignInSessions;
        int sessions = await db.CustomerSessions.Where(s => s.SignedInAtUtc < sessionCutoff).ExecuteDeleteAsync(ct);

        // After the legal period the order is kept only as an anonymous sales record.
        int orders = await db.Orders.Where(o => o.RetainUntilUtc <= now && o.BillingName != "-")
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.CustomerId, (int?)null)
                .SetProperty(o => o.BillingName, "-")
                .SetProperty(o => o.ShippingAddress, "-")
                .SetProperty(o => o.BillingNationalIdEncrypted, (string?)null)
                .SetProperty(o => o.BillingNationalIdIndex, (string?)null), ct);

        db.AddRange(
            new DestructionLogEntry { ExecutedAtUtc = now, Category = "sign-in-session", Method = "delete", RecordCount = sessions, Reason = "retention 180 days" },
            new DestructionLogEntry { ExecutedAtUtc = now, Category = "order-billing", Method = "anonymise", RecordCount = orders, Reason = "retention TTK 82" });
        await db.SaveChangesAsync(ct);
        return new DestructionRun(pending.Count, sessions, orders);
    }
}
