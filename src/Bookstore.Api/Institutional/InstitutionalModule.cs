using Bookstore.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Institutional;

public sealed record InstitutionUnitView(int Id, string Name, int? ParentUnitId);

public sealed record InstitutionView(int Id, string Name, string Code, string Kind, IReadOnlyList<InstitutionUnitView> Units);

public static class InstitutionalModule
{
    public static IServiceCollection AddInstitutionalModule(this IServiceCollection services) =>
        services.AddScoped<IInstitutionService, InstitutionService>();

    public static IEndpointRouteBuilder MapInstitutionalEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder institutions = app.MapGroup("/api/institutions").WithTags("Institutional");
        institutions.MapGet("/", GetInstitutions);
        return app;
    }

    private static async Task<Ok<List<InstitutionView>>> GetInstitutions(BookstoreContext db, CancellationToken ct) =>
        TypedResults.Ok(await db.Institutions.AsNoTracking()
            .OrderBy(i => i.Name)
            .Select(i => new InstitutionView(i.Id, i.Name, i.Code, i.Kind.ToString(),
                i.Units.OrderBy(u => u.Id).Select(u => new InstitutionUnitView(u.Id, u.Name, u.ParentUnitId)).ToList()))
            .ToListAsync(ct));
}
