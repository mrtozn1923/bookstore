using Bookstore.Data;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Privacy.Retention;

public sealed record ErasureResult(int CustomerId, int OrdersDetached, int SessionsDeleted, string[] CopiesErased);

public sealed class ErasureProcessor(
    BookstoreContext db, IEnumerable<IPersonalDataCopy> copies, IErasureLedger ledger, TimeProvider clock)
{
    public async Task<ErasureResult> EraseCustomerAsync(int customerId, CancellationToken ct = default)
    {
        DateTime now = clock.GetUtcNow().UtcDateTime;
        // ExecuteUpdate/ExecuteDelete bypass the change tracker; drop stale tracked copies first.
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Orders stay for the legal retention period but no longer point to a person.
        int orders = await db.Orders.Where(o => o.CustomerId == customerId)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.CustomerId, (int?)null), ct);
        int sessions = await db.CustomerSessions.Where(s => s.CustomerId == customerId).ExecuteDeleteAsync(ct);
        await db.Customers.Where(c => c.Id == customerId).ExecuteDeleteAsync(ct);
        await db.ErasureRequests.Where(r => r.CustomerId == customerId && r.CompletedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.CompletedAtUtc, now), ct);

        db.Add(new DestructionLogEntry { ExecutedAtUtc = now, Category = "customer-profile", Method = "delete", RecordCount = 1 + sessions, Reason = "erasure request" });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // Copies outside the database are erased after the commit; the ledger lets a restore replay this.
        List<string> erased = [];
        foreach (IPersonalDataCopy copy in copies)
        {
            await copy.EraseCustomerAsync(customerId, ct);
            erased.Add(copy.Name);
        }
        await ledger.AppendAsync(customerId, now, ct);
        return new ErasureResult(customerId, orders, sessions, [.. erased]);
    }

    // Run after restoring a backup: customers erased after the backup was taken come back otherwise.
    public async Task<int> ReapplyLedgerAsync(CancellationToken ct = default)
    {
        int count = 0;
        foreach (int id in await ledger.ErasedCustomerIdsAsync(ct))
        {
            if (await db.Customers.AnyAsync(c => c.Id == id, ct))
            {
                await EraseCustomerAsync(id, ct);
                count++;
            }
        }
        return count;
    }
}
