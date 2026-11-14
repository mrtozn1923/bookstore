using Bookstore.Data;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Ordering;

// Carts untouched for 30 days are deleted by a back-office job.
public sealed class CartCleanup(BookstoreContext db, TimeProvider clock)
{
    public static readonly TimeSpan MaxAge = TimeSpan.FromDays(30);

    public Task<int> DeleteExpiredAsync(CancellationToken ct)
    {
        DateTime cutoff = clock.GetUtcNow().UtcDateTime - MaxAge;
        return db.Carts.Where(c => c.UpdatedAtUtc < cutoff).ExecuteDeleteAsync(ct);
    }
}
