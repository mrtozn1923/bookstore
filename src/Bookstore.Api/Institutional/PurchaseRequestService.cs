using Bookstore.Catalog;
using Bookstore.Data;
using Bookstore.Inventory;
using Bookstore.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bookstore.Institutional;

public abstract record PurchaseOutcome
{
    public sealed record Done(PurchaseRequest Request) : PurchaseOutcome;
    public sealed record NotFound : PurchaseOutcome;
    public sealed record Forbidden(string Reason) : PurchaseOutcome;
    public sealed record Invalid(string Reason) : PurchaseOutcome;
    public sealed record Conflict(string Reason) : PurchaseOutcome;
}

public sealed record RequestLine(string Isbn, int Quantity);

public sealed class PurchaseRequestService(
    BookstoreContext db, ICatalogService catalog, IInventoryService inventory, TimeProvider clock)
{
    public const string AutoApprover = "unit-limit";

    public async Task<PurchaseOutcome> CreateAsync(InstitutionalCaller caller, IReadOnlyList<RequestLine> lines, CancellationToken ct)
    {
        if (!caller.IsBuyer)
        {
            return new PurchaseOutcome.Forbidden("Only an institution buyer can create a purchase request.");
        }
        PurchaseRequest request = new()
        {
            InstitutionId = caller.InstitutionId,
            UnitId = caller.UnitId,
            RequestedBy = caller.UserId,
            CreatedAtUtc = clock.GetUtcNow().UtcDateTime
        };
        if (await SetLinesAsync(request, lines, ct) is { } invalid)
        {
            return invalid;
        }
        await ApplyApprovalRuleAsync(request, ct);
        db.PurchaseRequests.Add(request);
        await db.SaveChangesAsync(ct);
        return new PurchaseOutcome.Done(request);
    }

    public async Task<PurchaseOutcome> ChangeLinesAsync(
        InstitutionalCaller caller, int requestId, IReadOnlyList<RequestLine> lines, CancellationToken ct)
    {
        PurchaseRequest? request = await FindAsync(caller, requestId, ct);
        if (request is null)
        {
            return new PurchaseOutcome.NotFound();
        }
        if (request.RequestedBy != caller.UserId)
        {
            return new PurchaseOutcome.Forbidden("Only the requester can change the lines.");
        }
        if (request.Status == PurchaseRequestStatus.Ordered)
        {
            return new PurchaseOutcome.Conflict("The request is already ordered.");
        }
        request.Lines.Clear();
        if (await SetLinesAsync(request, lines, ct) is { } invalid)
        {
            return invalid;
        }
        // A changed request is a new request: the old approval no longer applies (threat T10).
        await ApplyApprovalRuleAsync(request, ct);
        await db.SaveChangesAsync(ct);
        return new PurchaseOutcome.Done(request);
    }

    public async Task<PurchaseOutcome> ApproveAsync(InstitutionalCaller caller, int requestId, CancellationToken ct)
    {
        PurchaseRequest? request = await FindAsync(caller, requestId, ct);
        if (request is null)
        {
            return new PurchaseOutcome.NotFound();
        }
        if (!caller.IsApprover)
        {
            return new PurchaseOutcome.Forbidden("Only an institution approver can approve.");
        }
        // Four-eyes rule: holding both roles is not enough to approve your own request (threat T09).
        if (request.RequestedBy == caller.UserId)
        {
            return new PurchaseOutcome.Forbidden("The requester cannot approve their own request.");
        }
        if (request.Status != PurchaseRequestStatus.PendingApproval)
        {
            return new PurchaseOutcome.Conflict($"The request is {request.Status}, not PendingApproval.");
        }
        request.Status = PurchaseRequestStatus.Approved;
        request.ApprovedBy = caller.UserId;
        request.ApprovedAtUtc = clock.GetUtcNow().UtcDateTime;
        request.ApprovedLinesHash = request.ComputeLinesHash();
        await db.SaveChangesAsync(ct);
        return new PurchaseOutcome.Done(request);
    }

    public async Task<PurchaseOutcome> PlaceOrderAsync(InstitutionalCaller caller, int requestId, CancellationToken ct)
    {
        PurchaseRequest? request = await FindAsync(caller, requestId, ct);
        if (request is null)
        {
            return new PurchaseOutcome.NotFound();
        }
        if (!caller.IsBuyer)
        {
            return new PurchaseOutcome.Forbidden("Only an institution buyer can place the order.");
        }
        // The state is checked on the server; the client cannot skip the approval step (threat T11).
        if (request.Status != PurchaseRequestStatus.Approved)
        {
            return new PurchaseOutcome.Conflict($"The request is {request.Status}; only an approved request can be ordered.");
        }
        // Defence in depth: the lines must still be the ones that were approved (threat T10).
        if (request.ApprovedLinesHash != request.ComputeLinesHash())
        {
            return new PurchaseOutcome.Conflict("The request changed after approval.");
        }

        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(ct);
        ReservationResult reservation = await inventory.ReserveAsync(
            [.. request.Lines.Select(l => new StockLine(l.BookId, l.Quantity))], ct);
        if (!reservation.Succeeded)
        {
            return new PurchaseOutcome.Conflict("Not enough stock.");
        }

        Institution institution = await db.Institutions.SingleAsync(i => i.Id == request.InstitutionId, ct);
        Customer customer = await db.Customers.FirstOrDefaultAsync(c => c.Email == institution.BillingEmail, ct)
            ?? new Customer { Name = institution.Name, Email = institution.BillingEmail };
        IReadOnlyDictionary<int, BookInfo> books = await catalog.GetByIdsAsync([.. request.Lines.Select(l => l.BookId)], ct);
        Order order = new()
        {
            Customer = customer,
            InstitutionId = request.InstitutionId,
            PlacedAtUtc = clock.GetUtcNow().UtcDateTime,
            Status = OrderStatus.Placed
        };
        foreach (PurchaseRequestLine line in request.Lines)
        {
            order.Items.Add(new OrderItem
            {
                BookId = line.BookId,
                Title = books[line.BookId].Title,
                UnitPrice = line.UnitPrice,
                Quantity = line.Quantity,
                LineTotal = OrderItem.CalculateLineTotal(line.UnitPrice, line.Quantity)
            });
        }
        order.Total = order.Items.Sum(i => i.LineTotal);
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        request.Status = PurchaseRequestStatus.Ordered;
        request.OrderId = order.Id;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new PurchaseOutcome.Done(request);
    }

    public Task<PurchaseRequest?> FindAsync(InstitutionalCaller caller, int requestId, CancellationToken ct) =>
        // A request of another institution is "not found", never "forbidden": its existence is not disclosed.
        db.PurchaseRequests.Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == requestId && r.InstitutionId == caller.InstitutionId, ct);

    private async Task<PurchaseOutcome?> SetLinesAsync(PurchaseRequest request, IReadOnlyList<RequestLine> lines, CancellationToken ct)
    {
        if (lines.Count == 0 || lines.Any(l => l.Quantity <= 0) || lines.Sum(l => l.Quantity) < PurchaseRequest.MinimumCopies)
        {
            return new PurchaseOutcome.Invalid($"An institutional order needs at least {PurchaseRequest.MinimumCopies} copies.");
        }
        foreach (RequestLine line in lines)
        {
            BookInfo? book = await catalog.FindByIsbnAsync(line.Isbn, ct);
            if (book is null)
            {
                return new PurchaseOutcome.Invalid($"Book {line.Isbn} not found.");
            }
            // The price comes from the catalog, never from the request body.
            request.Lines.Add(new PurchaseRequestLine
            {
                BookId = book.Id,
                Quantity = line.Quantity,
                UnitPrice = PurchaseRequest.DiscountedPrice(book.Price)
            });
        }
        request.Total = request.Lines.Sum(l => OrderItem.CalculateLineTotal(l.UnitPrice, l.Quantity));
        return null;
    }

    private async Task ApplyApprovalRuleAsync(PurchaseRequest request, CancellationToken ct)
    {
        InstitutionUnit unit = await db.InstitutionUnits.SingleAsync(u => u.Id == request.UnitId, ct);
        // Pentest F-01: the limit applies to the unit's requests approved by the limit in the last 24 hours, not to one request.
        DateTime since = clock.GetUtcNow().UtcDateTime.AddHours(-24);
        List<decimal> recentTotals = await db.PurchaseRequests
            .Where(r => r.UnitId == request.UnitId && r.Id != request.Id
                && r.ApprovedBy == AutoApprover && r.CreatedAtUtc >= since)
            .Select(r => r.Total)
            .ToListAsync(ct);   // SQLite cannot SUM decimals in SQL; the list is small
        bool needsApproval = recentTotals.Sum() + request.Total > unit.ApprovalLimit;
        request.Status = needsApproval ? PurchaseRequestStatus.PendingApproval : PurchaseRequestStatus.Approved;
        request.ApprovedBy = needsApproval ? null : AutoApprover;
        request.ApprovedAtUtc = needsApproval ? null : clock.GetUtcNow().UtcDateTime;
        request.ApprovedLinesHash = needsApproval ? null : request.ComputeLinesHash();
    }
}
