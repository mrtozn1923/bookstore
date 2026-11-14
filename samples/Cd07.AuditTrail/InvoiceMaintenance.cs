using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bookstore.Institutional.Audit;

public sealed class InvoiceMaintenance(BookstoreContext db, AuditContext audit, TimeProvider clock)
{
    public async Task<int> PostponeDueDatesAsync(DateOnly before, int days, CancellationToken ct = default)
    {
        await using IDbContextTransaction tx = await db.Database.BeginTransactionAsync(ct);

        int count = await db.Invoices
            .Where(i => i.DueDate < before)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.DueDate, i => i.DueDate.AddDays(days)), ct);

        // ExecuteUpdate is invisible to the interceptor: write the summary audit entry ourselves
        db.AuditEntries.Add(new AuditEntry
        {
            EntityType = nameof(Invoice),
            EntityId = "*",
            Action = "BulkModified",
            Changes = JsonSerializer.Serialize(new { Filter = $"DueDate before {before:yyyy-MM-dd}", Property = "DueDate", ShiftDays = days, Rows = count }),
            UserName = audit.UserName,
            CorrelationId = audit.CorrelationId,
            Reason = audit.Reason,
            ChangedAt = clock.GetUtcNow()
        });
        await db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);
        return count;
    }
}
