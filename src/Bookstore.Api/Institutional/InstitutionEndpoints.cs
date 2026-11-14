using System.Security.Claims;
using Bookstore.Api.Common;
using Bookstore.Api.Identity;
using Bookstore.Api.Ordering;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Bookstore.Api.Institutional;

public static class InstitutionEndpoints
{
    public static IEndpointRouteBuilder MapInstitutionEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder institutions = app.MapGroup("/institutions/{institutionId}").RequireAuthorization();
        institutions.MapGet("", GetInstitution).WithName("getInstitution");
        institutions.MapGet("/orders", ListInstitutionOrders).WithName("listInstitutionOrders");
        return app;
    }

    private static Results<Ok<Institution>, ProblemHttpResult> GetInstitution(
        string institutionId, ClaimsPrincipal principal, InstitutionData data) =>
        Member(institutionId, principal, data) is { } institution
            ? TypedResults.Ok(institution)
            : InstitutionNotFound(institutionId);

    // A filtered view of the /orders collection; each item's canonical URL is /orders/{id}.
    private static readonly string[] ListParameters =
        ["unitId", "status", "createdAfter", "createdBefore", "sort", "pageSize", "pageToken"];

    private static Results<Ok<Page<OrderResponse>>, ProblemHttpResult> ListInstitutionOrders(
        HttpContext http, string institutionId, string? unitId, string? status, DateTime? createdAfter,
        DateTime? createdBefore, string? sort, int? pageSize, string? pageToken, ClaimsPrincipal principal,
        InstitutionData institutions, OrderData orders, Paging paging)
    {
        if (Paging.RejectUnknownParameters(http.Request, ListParameters) is { } unknown)
        {
            return unknown;
        }

        if (Member(institutionId, principal, institutions) is null)
        {
            return InstitutionNotFound(institutionId);
        }

        DemoUser user = principal.Current();
        if (user.Role == "InstitutionBuyer" && unitId is not null && unitId != user.UnitId)
        {
            return ProblemCatalog.Create(ProblemCatalog.UnitNotAllowed, [user.UnitId]);
        }

        // status=Pending,Confirmed: enum values are case-sensitive, as in the representation.
        List<OrderStatus> statuses = [];
        foreach (string name in (status ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Enum.TryParse(name, ignoreCase: false, out OrderStatus parsed) || !Enum.IsDefined(parsed))
            {
                return Paging.Invalid("status", "invalid-value");
            }

            statuses.Add(parsed);
        }

        if (Paging.ParseSort(sort ?? "-createdAt", OrderEndpoints.SortFields, out List<SortSpec<Order>> order) is { } badSort)
        {
            return badSort;
        }

        IEnumerable<Order> visible = orders.All.Where(o =>
            o.InstitutionId == institutionId && (unitId is null || o.UnitId == unitId) && OrderData.CanSee(user, o) &&
            (statuses.Count == 0 || statuses.Contains(o.Status)) &&
            (createdAfter is null || o.CreatedAt >= createdAfter.Value.ToUniversalTime()) &&
            (createdBefore is null || o.CreatedAt < createdBefore.Value.ToUniversalTime()));
        string query = string.Join('|', user.Name, institutionId, unitId, string.Join(',', statuses),
            createdAfter?.ToString("O"), createdBefore?.ToString("O"));
        return paging.Paginate(http, visible, order, OrderEndpoints.ById, query, pageSize, pageToken, OrderResponse.From);
    }

    private static Institution? Member(string institutionId, ClaimsPrincipal principal, InstitutionData data)
    {
        DemoUser user = principal.Current();
        return user.InstitutionId == institutionId || user.Role == "StoreStaff" ? data.Find(institutionId) : null;
    }

    private static ProblemHttpResult InstitutionNotFound(string institutionId) =>
        ProblemCatalog.Create(ProblemCatalog.NotFound, [$"/institutions/{institutionId}"]);
}
