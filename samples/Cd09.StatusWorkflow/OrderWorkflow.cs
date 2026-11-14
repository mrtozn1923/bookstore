using Microsoft.EntityFrameworkCore;

namespace Bookstore.Institutional.Workflow;

public enum TransitionResult { Done, NotFound, Forbidden, CommentRequired, InvalidTransition, Conflict }

public sealed class OrderWorkflow(BookstoreContext db, TimeProvider clock)
{
    public async Task<IReadOnlyList<string>> AllowedActionsAsync(int orderId, UserInfo user, CancellationToken ct = default)
    {
        OrderStatus? status = await db.Orders.Where(o => o.Id == orderId).Select(o => (OrderStatus?)o.Status).SingleOrDefaultAsync(ct);
        if (status is null)
        {
            return [];
        }
        return await db.StatusTransitions
            .Where(t => t.FromStatus == status && user.Roles.Contains(t.RequiredRole))
            .OrderBy(t => t.Action)
            .Select(t => t.Action)
            .ToListAsync(ct);
    }

    public async Task<TransitionResult> ExecuteAsync(int orderId, string action, UserInfo user, string? comment, CancellationToken ct = default)
    {
        Order? order = await db.Orders.SingleOrDefaultAsync(o => o.Id == orderId, ct);
        return order is null ? TransitionResult.NotFound : await ExecuteAsync(order, action, user, comment, ct);
    }

    public async Task<TransitionResult> ExecuteAsync(Order order, string action, UserInfo user, string? comment, CancellationToken ct = default)
    {
        StatusTransition? transition = await db.StatusTransitions
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.FromStatus == order.Status && t.Action == action, ct);
        if (transition is null)
        {
            return TransitionResult.InvalidTransition;
        }
        if (!user.Roles.Contains(transition.RequiredRole))
        {
            return TransitionResult.Forbidden;
        }
        if (transition.CommentRequired && string.IsNullOrWhiteSpace(comment))
        {
            return TransitionResult.CommentRequired;
        }

        db.StatusHistory.Add(new StatusHistory
        {
            OrderId = order.Id,
            FromStatus = order.Status,
            ToStatus = transition.ToStatus,
            ChangedBy = user.Name,
            ChangedAt = clock.GetUtcNow(),
            Comment = comment
        });
        order.Status = transition.ToStatus;

        try
        {
            await db.SaveChangesAsync(ct);
            return TransitionResult.Done;
        }
        catch (DbUpdateConcurrencyException)
        {
            // someone else changed this order's status before us; the rowversion didn't match
            db.ChangeTracker.Clear();
            return TransitionResult.Conflict;
        }
    }
}
