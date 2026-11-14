namespace Bookstore.Api.Institutional;

public record InstitutionContext(InstitutionUnit Unit)
{
    // Parametre tipinin kendisi binding'i üstleniyor
    public static ValueTask<InstitutionContext?> BindAsync(HttpContext context)
    {
        InstitutionDirectory directory = context.RequestServices.GetRequiredService<InstitutionDirectory>();
        string? raw = context.Request.Headers[TenantMiddleware.HeaderName];

        InstitutionContext? result = int.TryParse(raw, out int unitId) && directory.Find(unitId) is { } unit
            ? new InstitutionContext(unit)
            : null;
        return ValueTask.FromResult(result);
    }
}
