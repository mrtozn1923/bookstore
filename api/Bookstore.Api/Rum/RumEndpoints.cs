namespace Bookstore.Api.Rum;

public static class RumEndpoints
{
    private const int MaxReportsPerBeacon = 20;

    public static IEndpointRouteBuilder MapRumEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/rum", Results<NoContent, BadRequest> (RumReport[] reports, RumStore store) =>
        {
            if (reports.Length is 0 or > MaxReportsPerBeacon)
            {
                return TypedResults.BadRequest();
            }
            foreach (RumReport report in reports)
            {
                // Ignore unknown metrics and absurd values instead of storing them
                if (RumThresholds.IsKnown(report.Name) && report.Value is >= 0 and < 120_000 && report.Route.Length <= 100)
                {
                    store.Add(report);
                }
            }
            return TypedResults.NoContent();
        });

        app.MapGet("/api/rum/p75", (RumStore store, int? minutes, int? minSamples) =>
            TypedResults.Ok(store.P75(TimeSpan.FromMinutes(minutes ?? 60), minSamples ?? 20)));

        return app;
    }
}
