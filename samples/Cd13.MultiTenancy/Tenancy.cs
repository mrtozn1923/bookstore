using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bookstore.Institutional.MultiTenancy;

public interface ITenantContext
{
    int TenantId { get; }
}

public sealed class TenantContext : ITenantContext
{
    private int? _tenantId;

    public int TenantId => _tenantId
        ?? throw new InvalidOperationException("Tenant is not resolved for this scope.");

    public void Set(int tenantId)
    {
        if (_tenantId is not null && _tenantId != tenantId)
            throw new InvalidOperationException("Tenant cannot change within a scope.");
        _tenantId = tenantId;
    }
}

// A fixed tenant, for consoles and tests
public sealed class FixedTenant(int tenantId) : ITenantContext
{
    public int TenantId => tenantId;
}

public sealed record TenantInfo(int Id, string Subdomain, string Name);

public interface ITenantCatalog
{
    TenantInfo? FindBySubdomain(string subdomain);
}

public sealed class InMemoryTenantCatalog : ITenantCatalog
{
    private static readonly TenantInfo[] Tenants =
    [
        new(1, "sample-school", "Sample School"),
        new(2, "sample-library", "Sample Library")
    ];

    public TenantInfo? FindBySubdomain(string subdomain) =>
        Tenants.FirstOrDefault(t => t.Subdomain.Equals(subdomain, StringComparison.OrdinalIgnoreCase));
}

public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext http, ITenantCatalog catalog, TenantContext tenant)
    {
        string subdomain = http.Request.Host.Host.Split('.')[0];
        TenantInfo? info = catalog.FindBySubdomain(subdomain);
        if (info is null)
        {
            http.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        // The institution in the user's token must match the one from the host
        string? claim = http.User.FindFirst("institution_id")?.Value;
        if (http.User.Identity?.IsAuthenticated == true && claim != info.Id.ToString())
        {
            http.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        tenant.Set(info.Id);
        await next(http);
    }
}

public sealed class TenantSessionContextInterceptor(ITenantContext tenant) : DbConnectionInterceptor
{
    public override async Task ConnectionOpenedAsync(
        DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "EXEC sp_set_session_context @key = N'TenantId', @value = @tenantId, @read_only = 1;";
        cmd.Parameters.Add(new SqlParameter("@tenantId", tenant.TenantId));
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}

public static class NightlyApprovalReminderJob
{
    public static async Task RunAsync(IServiceScopeFactory scopes, int[] tenantIds, CancellationToken ct = default)
    {
        foreach (int tenantId in tenantIds)
        {
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<TenantContext>().Set(tenantId);

            BookstoreContext db = scope.ServiceProvider.GetRequiredService<BookstoreContext>();
            int pending = await db.PurchaseApprovals.CountAsync(a => a.Status == ApprovalStatus.Pending, ct);
            Console.WriteLine($"job tenant {tenantId}: {pending} pending approvals");
        }
    }
}
