using System.Globalization;
using Bookstore.Institutional.Features;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.AspNetCore;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
string flagsFile = Path.Combine(AppContext.BaseDirectory, "flags.json");

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:5315");
builder.Configuration.AddJsonFile(flagsFile, optional: false, reloadOnChange: true);

builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
builder.Services.AddScopedFeatureManagement()
    .AddFeatureFilter<TenantFilter>();
builder.Services.AddScoped<SingleStepApproval>();
builder.Services.AddScoped<TwoLevelApproval>();
builder.Services.AddScoped<PurchaseApprovalPolicyRouter>();

WebApplication app = builder.Build();

// Demo only: the tenant comes from a header (post 13 resolves it from the subdomain)
app.Use(async (http, next) =>
{
    if (int.TryParse(http.Request.Headers["X-Demo-Tenant"], out int id))
        http.RequestServices.GetRequiredService<TenantContext>().Set(id);
    await next(http);
});

app.MapGet("/api/orders/{id:int}/approval-steps", async (
    int id, decimal total, PurchaseApprovalPolicyRouter router, CancellationToken ct) =>
{
    IPurchaseApprovalPolicy policy = await router.ResolveAsync(ct);
    return Results.Ok(new { policy = policy.Name, steps = policy.StepsFor(new ApprovalRequest(id, total)) });
});

app.MapGet("/api/dashboard-layout", async (IVariantFeatureManager features, IVariantFeatureManagerSnapshot snapshot, CancellationToken ct) =>
{
    string plain = "", snap = "";
    for (int i = 0; i < 10; i++)
    {
        plain += await features.IsEnabledAsync(FeatureFlags.DashboardLayoutB, ct) ? "B" : "A";
        snap += await snapshot.IsEnabledAsync(FeatureFlags.DashboardLayoutB, ct) ? "B" : "A";
    }
    return Results.Ok(new { plain, snapshot = snap });
});

app.MapPost("/api/invoices/{period}/pdf", (string period) => Results.Ok(new { period, status = "queued" }))
    .WithFeatureGate(FeatureFlags.InvoicePdfExport);

app.MapGet("/api/features", async (IVariantFeatureManagerSnapshot features, CancellationToken ct) =>
{
    string[] clientFlags = [FeatureFlags.PurchaseApprovalV2, FeatureFlags.InvoicePdfExport];
    Dictionary<string, bool> result = [];
    foreach (string flag in clientFlags)
        result[flag] = await features.IsEnabledAsync(flag, ct);
    return result;
});

await app.StartAsync();

// 1) The opening: a misspelled flag name
await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    scope.ServiceProvider.GetRequiredService<TenantContext>().Set(3);
    IVariantFeatureManager fm = scope.ServiceProvider.GetRequiredService<IVariantFeatureManager>();
    bool typo = await fm.IsEnabledAsync("PurchaseAprovalV2");
    Console.WriteLine($"typo flag: {typo}");
}

using HttpClient http = new() { BaseAddress = new Uri("http://127.0.0.1:5315") };
async Task Call(int tenant, HttpMethod method, string path)
{
    using HttpRequestMessage req = new(method, path);
    req.Headers.Add("X-Demo-Tenant", tenant.ToString());
    using HttpResponseMessage res = await http.SendAsync(req);
    Console.WriteLine($"tenant {tenant} {method} {path} -> {(int)res.StatusCode} {await res.Content.ReadAsStringAsync()}");
}

await Call(3, HttpMethod.Get, "/api/orders/107/approval-steps?total=62000");
await Call(1, HttpMethod.Get, "/api/orders/107/approval-steps?total=62000");
await Call(1, HttpMethod.Get, "/api/dashboard-layout");
await Call(1, HttpMethod.Get, "/api/dashboard-layout");
await Call(3, HttpMethod.Get, "/api/features");
await Call(1, HttpMethod.Get, "/api/features");

// 2) Kill switch: switch the flag off in the file while the app is running
await Call(3, HttpMethod.Post, "/api/invoices/2026-10/pdf");
string json = await File.ReadAllTextAsync(flagsFile);
await File.WriteAllTextAsync(flagsFile, json.Replace(
    "\"id\": \"InvoicePdfExport\",\n        \"enabled\": true", "\"id\": \"InvoicePdfExport\",\n        \"enabled\": false"));
await Task.Delay(TimeSpan.FromSeconds(3));
await Call(3, HttpMethod.Post, "/api/invoices/2026-10/pdf");
await Call(3, HttpMethod.Get, "/api/features");
await File.WriteAllTextAsync(flagsFile, json);
await app.StopAsync();

// 3) Flag debt: owner and expiry date for every flag
DateOnly today = DateOnly.FromDateTime(TimeProvider.System.GetLocalNow().DateTime);
Console.WriteLine($"today: {today:yyyy-MM-dd}");
foreach ((string name, FlagInfoAttribute info) in FeatureFlags.All())
{
    string state = info.Expires < today ? "EXPIRED" : "ok";
    Console.WriteLine($"{name,-20} {info.Kind,-10} {info.Owner,-19} {info.Expires:yyyy-MM-dd} {state}");
}
