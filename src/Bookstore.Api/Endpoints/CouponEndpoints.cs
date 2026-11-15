using Bookstore.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Api.Endpoints;

public sealed record CouponDto(string Code, int PercentOff, DateTimeOffset ValidUntil);

public sealed record CreateCouponRequest(string Code, int PercentOff, DateTimeOffset ValidUntil);

public static class CouponEndpoints
{
    public static IEndpointRouteBuilder MapCouponEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder coupons = app.MapGroup("/api/coupons").WithTags("Coupons").RequireAuthorization();

        coupons.MapGet("/{code}", async Task<Results<Ok<CouponDto>, NotFound>> (
            string code, BookstoreDbContext db, TimeProvider clock, CancellationToken ct) =>
        {
            Coupon? coupon = await FindValidAsync(db, code, clock.GetUtcNow(), ct);
            return coupon is null ? TypedResults.NotFound() : TypedResults.Ok(ToDto(coupon));
        });

        coupons.MapPost("/", async Task<Results<Created<CouponDto>, ValidationProblem, Conflict<string>>> (
            CreateCouponRequest request, BookstoreDbContext db, CancellationToken ct) =>
        {
            if (request.PercentOff is < 1 or > 100)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["percentOff"] = ["PercentOff must be between 1 and 100."]
                });
            }
            if (await db.Coupons.AnyAsync(c => c.Code == request.Code, ct))
            {
                return TypedResults.Conflict($"Coupon {request.Code} already exists.");
            }

            Coupon coupon = new() { Code = request.Code, PercentOff = request.PercentOff, ValidUntil = request.ValidUntil };
            db.Coupons.Add(coupon);
            await db.SaveChangesAsync(ct);
            return TypedResults.Created($"/api/coupons/{coupon.Code}", ToDto(coupon));
        }).RequireAuthorization("CatalogAdmin");

        return app;
    }

    public static Task<Coupon?> FindValidAsync(BookstoreDbContext db, string code, DateTimeOffset now, CancellationToken ct) =>
        db.Coupons.AsNoTracking().SingleOrDefaultAsync(c => c.Code == code && c.ValidUntil >= now, ct);

    private static CouponDto ToDto(Coupon coupon) => new(coupon.Code, coupon.PercentOff, coupon.ValidUntil);
}
