using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bookstore.Institutional.SoftDelete;

public enum RestoreResult
{
    Restored,
    NotFound,
    EmailInUse
}

public class InstitutionUserLifecycle(BookstoreContext db, TimeProvider clock)
{
    public async Task<Guid> SoftDeleteAsync(int userId, string deletedBy, CancellationToken ct = default)
    {
        Guid deletionId = Guid.CreateVersion7();
        DateTimeOffset now = clock.GetUtcNow();

        await using IDbContextTransaction tx = await db.Database.BeginTransactionAsync(ct);

        int users = await db.InstitutionUsers
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.DeletedAt, now)
                .SetProperty(u => u.DeletedBy, deletedBy)
                .SetProperty(u => u.DeletionId, deletionId), ct);
        if (users == 0)
        {
            throw new InvalidOperationException($"Institution user {userId} not found.");
        }

        // Only pending approval requests go with the user; approved ones are order and invoice history and stay
        await db.PurchaseApprovals
            .Where(a => a.RequestedById == userId && a.Status == ApprovalStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.DeletedAt, now)
                .SetProperty(a => a.DeletedBy, deletedBy)
                .SetProperty(a => a.DeletionId, deletionId), ct);

        await tx.CommitAsync(ct);
        return deletionId;
    }

    public async Task<RestoreResult> RestoreAsync(Guid deletionId, CancellationToken ct = default)
    {
        await using IDbContextTransaction tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            int users = await db.InstitutionUsers
                .IgnoreQueryFilters([BookstoreContext.SoftDeleteFilter])
                .Where(u => u.DeletionId == deletionId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(u => u.DeletedAt, (DateTimeOffset?)null)
                    .SetProperty(u => u.DeletedBy, (string?)null)
                    .SetProperty(u => u.DeletionId, (Guid?)null), ct);
            if (users == 0)
            {
                return RestoreResult.NotFound;
            }

            await db.PurchaseApprovals
                .IgnoreQueryFilters([BookstoreContext.SoftDeleteFilter])
                .Where(a => a.DeletionId == deletionId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.DeletedAt, (DateTimeOffset?)null)
                    .SetProperty(a => a.DeletedBy, (string?)null)
                    .SetProperty(a => a.DeletionId, (Guid?)null), ct);

            await tx.CommitAsync(ct);
            return RestoreResult.Restored;
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            // Filtered unique index: an active user with the same e-mail already exists
            await tx.RollbackAsync(ct);
            return RestoreResult.EmailInUse;
        }
    }

    public async Task EraseAsync(int userId, CancellationToken ct = default)
    {
        // KVKK/GDPR erasure request: the row stays (FKs, order history), personal data does not
        await db.InstitutionUsers
            .IgnoreQueryFilters([BookstoreContext.SoftDeleteFilter])
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.FullName, "Erased User")
                .SetProperty(u => u.Email, u => "erased-" + u.Id + "@invalid")
                .SetProperty(u => u.Phone, (string?)null), ct);
    }
}

public sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
