using Microsoft.AspNetCore.Http.Features;

namespace Bookstore.Analytics.Web.Analytics;

public static class ConsentEndpoints
{
    public static IEndpointRouteBuilder MapAnalyticsConsent(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/consent/analytics");

        // writes the .AspNet.Consent cookie
        group.MapPost("/grant", (HttpContext context) =>
        {
            context.Features.Get<ITrackingConsentFeature>()!.GrantConsent();
            return Results.NoContent();
        });

        group.MapPost("/withdraw", (HttpContext context) =>
        {
            context.Features.Get<ITrackingConsentFeature>()!.WithdrawConsent();
            // GA4 sets _ga and _ga_<container-id>; ours is bs_ga_cid
            foreach (string name in context.Request.Cookies.Keys
                         .Where(n => n == "_ga" || n.StartsWith("_ga_", StringComparison.Ordinal) || n == ClientIdCookie.Name))
            {
                context.Response.Cookies.Delete(name);
            }

            return Results.NoContent();
        });

        return app;
    }

    public static bool CanTrack(this HttpContext context) =>
        context.Features.Get<ITrackingConsentFeature>()?.CanTrack ?? false;
}
