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
    private static Results<Ok<Page<OrderResponse>>, ProblemHttpResult> ListInstitutionOrders(
        string institutionId, string? unitId, int? pageSize, string? pageToken, ClaimsPrincipal principal,
        InstitutionData institutions, OrderData orders)
    {
        if (Member(institutionId, principal, institutions) is null)
        {
            return InstitutionNotFound(institutionId);
        }

        DemoUser user = principal.Current();
        if (user.Role == "InstitutionBuyer" && unitId is not null && unitId != user.UnitId)
        {
            return ProblemCatalog.Create(ProblemCatalog.UnitNotAllowed, [user.UnitId]);
        }

        List<OrderResponse> items =
        [
            .. orders.All
                .Where(o => o.InstitutionId == institutionId && (unitId is null || o.UnitId == unitId))
                .Where(o => OrderData.CanSee(user, o))
                .OrderByDescending(o => o.CreatedAt)
                .Select(OrderResponse.From),
        ];
        return Paging.Paginate(items, pageSize, pageToken);
    }

    private static Institution? Member(string institutionId, ClaimsPrincipal principal, InstitutionData data)
    {
        DemoUser user = principal.Current();
        return user.InstitutionId == institutionId || user.Role == "StoreStaff" ? data.Find(institutionId) : null;
    }

    private static ProblemHttpResult InstitutionNotFound(string institutionId) =>
        ProblemCatalog.Create(ProblemCatalog.NotFound, [$"/institutions/{institutionId}"]);
}
