using Microsoft.EntityFrameworkCore;

namespace Bookstore.Ordering.Paging;

public sealed record OrderCursor(int InstitutionId, DateTime CreatedAt, int Id, bool Backward);

public sealed record OrderRow(int Id, DateTime CreatedAt, decimal TotalAmount);

public sealed record CursorPage<T>(IReadOnlyList<T> Items, string? Next, string? Previous);

public sealed class InvalidCursorException() : Exception("The cursor is invalid or belongs to another query.");

public sealed class OrderPager(BookstoreContext db, CursorCodec codec)
{
    public IQueryable<Order> BuildQuery(int institutionId, OrderCursor? cursor)
    {
        IQueryable<Order> query = db.Orders.AsNoTracking().Where(o => o.InstitutionId == institutionId);

        if (cursor is { Backward: false } after)
        {
            // (CreatedAt, Id) < (@c, @i) cannot be written in SQL Server; the expanded form:
            query = query.Where(o => o.CreatedAt < after.CreatedAt
                || (o.CreatedAt == after.CreatedAt && o.Id < after.Id));
        }
        else if (cursor is { Backward: true } before)
        {
            query = query.Where(o => o.CreatedAt > before.CreatedAt
                || (o.CreatedAt == before.CreatedAt && o.Id > before.Id));
        }

        return cursor is { Backward: true }
            ? query.OrderBy(o => o.CreatedAt).ThenBy(o => o.Id)
            : query.OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id);
    }

    public async Task<CursorPage<OrderRow>> GetPageAsync(int institutionId, string? token, int pageSize, CancellationToken ct = default)
    {
        OrderCursor? cursor = null;
        if (token is not null && (!codec.TryDecode(token, out cursor) || cursor.InstitutionId != institutionId))
        {
            throw new InvalidCursorException();
        }

        List<OrderRow> rows = await BuildQuery(institutionId, cursor)
            .Take(pageSize + 1)
            .Select(o => new OrderRow(o.Id, o.CreatedAt, o.TotalAmount))
            .ToListAsync(ct);

        // we asked for one extra row: the COUNT-free answer to "is there more"
        bool more = rows.Count > pageSize;
        if (more)
        {
            rows.RemoveAt(rows.Count - 1);
        }

        bool backward = cursor?.Backward ?? false;
        if (backward)
        {
            rows.Reverse();
        }

        bool hasNext = backward || more;
        bool hasPrevious = backward ? more : cursor is not null;
        return new CursorPage<OrderRow>(
            rows,
            hasNext && rows.Count > 0 ? codec.Encode(new OrderCursor(institutionId, rows[^1].CreatedAt, rows[^1].Id, false)) : null,
            hasPrevious && rows.Count > 0 ? codec.Encode(new OrderCursor(institutionId, rows[0].CreatedAt, rows[0].Id, true)) : null);
    }
}
