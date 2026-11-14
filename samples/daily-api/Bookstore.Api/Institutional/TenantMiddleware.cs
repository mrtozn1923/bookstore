namespace Bookstore.Api.Institutional;

public class TenantMiddleware(RequestDelegate next, InstitutionDirectory directory)
{
    public const string HeaderName = "X-Tenant-Id";
    private static readonly object UnitKey = new();

    public async Task InvokeAsync(HttpContext context)
    {
        // Yalnız kurumsal endpoint'ler tenant ister
        if (!context.Request.Path.StartsWithSegments("/api/institution"))
        {
            await next(context);
            return;
        }

        string? raw = context.Request.Headers[HeaderName];
        if (!int.TryParse(raw, out int unitId) || directory.Find(unitId) is not { } unit)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = $"Missing or unknown {HeaderName} header." });
            return;
        }

        context.Items[UnitKey] = unit;
        await next(context);
    }

    public static InstitutionUnit GetUnit(HttpContext context) =>
        context.Items[UnitKey] as InstitutionUnit
            ?? throw new InvalidOperationException("TenantMiddleware did not run for this request.");
}
