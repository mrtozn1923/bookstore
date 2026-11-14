using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Bookstore.Api.Catalog;
using Bookstore.Api.Common;
using Bookstore.Api.Identity;
using Bookstore.Api.Ordering;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Bookstore.Api.Institutional;

public enum ApprovalStatus { Pending, Approved, Rejected }

public sealed class PurchaseApproval
{
    public required Guid Id { get; init; }
    public required Guid OrderId { get; init; }
    public required string InstitutionId { get; init; }
    public required string UnitId { get; init; }
    public required decimal Amount { get; init; }
    public required DateTime RequestedAt { get; init; }
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public string? DecidedBy { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? Reason { get; set; }
    public int Version { get; set; } = 1;
    public string ETag => $"\"{Version}\"";
}

public sealed record PurchaseApprovalResponse(
    Guid Id, Guid OrderId, string InstitutionId, string UnitId, Money Amount, ApprovalStatus Status,
    DateTime RequestedAt, string? DecidedBy, DateTime? DecidedAt, string? Reason)
{
    public static PurchaseApprovalResponse From(PurchaseApproval a) =>
        new(a.Id, a.OrderId, a.InstitutionId, a.UnitId, new Money(a.Amount, "TRY"), a.Status, a.RequestedAt, a.DecidedBy,
            a.DecidedAt, a.Reason);
}

public sealed record RejectApprovalRequest(
    [property: Required(ErrorMessage = "required"), StringLength(500, MinimumLength = 3, ErrorMessage = "invalid-length")]
    string Reason);

// Every Pending institutional order waits for an approver of its institution (ADR-0008).
public sealed class PurchaseApprovalData
{
    private readonly ConcurrentDictionary<Guid, PurchaseApproval> _approvals = new();

    public PurchaseApprovalData(OrderData orders)
    {
        foreach (Order order in orders.All.Where(o => o.InstitutionId is not null && o.Status == OrderStatus.Pending))
        {
            Request(order, order.CreatedAt);
        }
    }

    public IEnumerable<PurchaseApproval> All => _approvals.Values;

    public PurchaseApproval Request(Order order, DateTime requestedAt)
    {
        PurchaseApproval approval = new()
        {
            Id = Guid.CreateVersion7(new DateTimeOffset(requestedAt, TimeSpan.Zero)), OrderId = order.Id,
            InstitutionId = order.InstitutionId!, UnitId = order.UnitId!, Amount = order.Total, RequestedAt = requestedAt,
        };
        _approvals[approval.Id] = approval;
        return approval;
    }

    public PurchaseApproval? Find(Guid id) => _approvals.GetValueOrDefault(id);
}

public static class PurchaseApprovalEndpoints
{
    private static readonly SortField<PurchaseApproval>[] SortFields = [new("requestedAt", a => a.RequestedAt)];
    private static readonly SortField<PurchaseApproval> ById = new("id", a => a.Id.ToString());

    public static IEndpointRouteBuilder MapPurchaseApprovalEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/institutions/{institutionId}/purchase-approvals", List).WithName("listPurchaseApprovals")
            .RequireAuthorization();
        RouteGroupBuilder approvals = app.MapGroup("/purchase-approvals").RequireAuthorization();
        approvals.MapGet("/{approvalId:guid}", Get).WithName("getPurchaseApproval");
        approvals.MapPost("/{approvalId:guid}:approve", Approve).WithName("approvePurchase");
        approvals.MapPost("/{approvalId:guid}:reject", Reject).WithName("rejectPurchase");
        return app;
    }

    private static Results<Ok<Page<PurchaseApprovalResponse>>, ProblemHttpResult> List(
        HttpContext http, string institutionId, string? status, int? pageSize, string? pageToken, ClaimsPrincipal principal,
        PurchaseApprovalData data, Paging paging)
    {
        if (Paging.RejectUnknownParameters(http.Request, ["status", "pageSize", "pageToken"]) is { } unknown)
        {
            return unknown;
        }

        DemoUser user = principal.Current();
        if (user.InstitutionId != institutionId && user.Role != "StoreStaff")
        {
            return ProblemCatalog.Create(ProblemCatalog.NotFound, [$"/institutions/{institutionId}"]);
        }

        ApprovalStatus? wanted = null;
        if (status is not null)
        {
            if (!Enum.TryParse(status, ignoreCase: false, out ApprovalStatus parsed) || !Enum.IsDefined(parsed))
            {
                return Paging.Invalid("status", "invalid-value");
            }

            wanted = parsed;
        }

        IEnumerable<PurchaseApproval> visible = data.All.Where(a =>
            a.InstitutionId == institutionId && CanSee(user, a) && (wanted is null || a.Status == wanted));
        _ = Paging.ParseSort("-requestedAt", SortFields, out List<SortSpec<PurchaseApproval>> order);
        return paging.Paginate(http, visible, order, ById, $"{user.Name}|{institutionId}|{wanted}", pageSize, pageToken,
            PurchaseApprovalResponse.From);
    }

    private static Results<Ok<PurchaseApprovalResponse>, ProblemHttpResult> Get(
        Guid approvalId, ClaimsPrincipal principal, PurchaseApprovalData data, HttpResponse response)
    {
        if (Visible(approvalId, principal, data) is not { } approval)
        {
            return NotFound(approvalId);
        }

        response.Headers.ETag = approval.ETag;
        return TypedResults.Ok(PurchaseApprovalResponse.From(approval));
    }

    private static Results<Ok<PurchaseApprovalResponse>, ProblemHttpResult> Approve(
        Guid approvalId, ClaimsPrincipal principal, PurchaseApprovalData data, OrderData orders, HttpRequest request,
        HttpResponse response) =>
        Decide(approvalId, principal, data, orders, request, response, ApprovalStatus.Approved, null);

    private static Results<Ok<PurchaseApprovalResponse>, ProblemHttpResult> Reject(
        Guid approvalId, RejectApprovalRequest body, ClaimsPrincipal principal, PurchaseApprovalData data, OrderData orders,
        HttpRequest request, HttpResponse response) =>
        Decide(approvalId, principal, data, orders, request, response, ApprovalStatus.Rejected, body.Reason);

    // A decision is a one-shot state change (custom method, ADR-0002) and a conditional write (ADR-0005).
    private static Results<Ok<PurchaseApprovalResponse>, ProblemHttpResult> Decide(
        Guid approvalId, ClaimsPrincipal principal, PurchaseApprovalData data, OrderData orders, HttpRequest request,
        HttpResponse response, ApprovalStatus decision, string? reason)
    {
        DemoUser user = principal.Current();
        if (Visible(approvalId, principal, data) is not { } approval)
        {
            return NotFound(approvalId);
        }

        // The buyer can see the approval, so 403 rather than 404 (BS-45).
        if (user.Role != "InstitutionApprover")
        {
            return ProblemCatalog.Create(ProblemCatalog.ApproverRequired);
        }

        lock (approval)
        {
            string? ifMatch = request.Headers.IfMatch;
            if (ifMatch is null)
            {
                return ProblemCatalog.Create(ProblemCatalog.PreconditionRequired);
            }

            if (ifMatch != approval.ETag && ifMatch != "*")
            {
                return ProblemCatalog.Create(ProblemCatalog.PreconditionFailed, [ifMatch, approval.ETag]);
            }

            if (approval.Status != ApprovalStatus.Pending)
            {
                return ProblemCatalog.Create(ProblemCatalog.ApprovalAlreadyDecided, [approvalId, approval.Status]);
            }

            approval.Status = decision;
            approval.DecidedBy = user.Name;
            approval.DecidedAt = DateTime.UtcNow;
            approval.Reason = reason;
            approval.Version++;
            orders.Find(approval.OrderId)!.Status = decision == ApprovalStatus.Approved ? OrderStatus.Confirmed : OrderStatus.Cancelled;
        }

        response.Headers.ETag = approval.ETag;
        return TypedResults.Ok(PurchaseApprovalResponse.From(approval));
    }

    private static bool CanSee(DemoUser user, PurchaseApproval approval) => user.Role switch
    {
        "InstitutionApprover" => approval.InstitutionId == user.InstitutionId,
        "InstitutionBuyer" => approval.InstitutionId == user.InstitutionId && approval.UnitId == user.UnitId,
        "StoreStaff" => true,
        _ => false,
    };

    private static PurchaseApproval? Visible(Guid approvalId, ClaimsPrincipal principal, PurchaseApprovalData data) =>
        data.Find(approvalId) is { } approval && CanSee(principal.Current(), approval) ? approval : null;

    private static ProblemHttpResult NotFound(Guid approvalId) =>
        ProblemCatalog.Create(ProblemCatalog.NotFound, [$"/purchase-approvals/{approvalId}"]);
}
