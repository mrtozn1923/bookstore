using Microsoft.AspNetCore.Http.HttpResults;

namespace Bookstore.Institutional;

public sealed record InstitutionUnitView(int Id, string Name, string Kind);

// Read-only for now: the units of the demo institution until the Institutional module gets its own tables.
public static class InstitutionalModule
{
    private static readonly InstitutionUnitView[] DemoUnits =
    [
        new(1, "Main Campus Library", "Campus"),
        new(2, "North Campus Library", "Campus"),
        new(3, "Science Department", "Department"),
        new(4, "Literature Department", "Department"),
        new(5, "Primary School Branch", "Branch")
    ];

    public static IEndpointRouteBuilder MapInstitutionalEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/institutions/{institutionId:int}/units", GetUnits).WithTags("Institutional");
        return app;
    }

    private static Results<Ok<InstitutionUnitView[]>, NotFound> GetUnits(int institutionId) =>
        institutionId == 1 ? TypedResults.Ok(DemoUnits) : TypedResults.NotFound();
}
