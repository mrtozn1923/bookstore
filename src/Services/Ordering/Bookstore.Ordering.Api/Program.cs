using Asp.Versioning;
using Bookstore.Inventory.V1;
using Bookstore.Ordering.CatalogCopy;
using Bookstore.Ordering.Data;
using Bookstore.Ordering.Orders;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddSqlServerDbContext<OrderingDbContext>("orderingdb");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<PlaceOrderHandler>();
builder.Services.AddScoped<OrderQueries>();
// Used only by the migrator for the initial load; the running service never calls Catalog.
builder.Services.AddHttpClient<CatalogClient>(client => client.BaseAddress = new Uri("https+http://catalog"));
// gRPC client through IHttpClientFactory: service discovery and the standard resilience handler apply to it too.
// "_grpc." picks Inventory's endpoint named grpc. A gRPC channel accepts only http/https addresses,
// so the "https+http" fallback form cannot be used here.
builder.Services.AddGrpcClient<InventoryService.InventoryServiceClient>(options =>
    options.Address = new Uri("http://_grpc.inventory"));

builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddApiVersioning(options =>
    {
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
        options.ReportApiVersions = true;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    })
    .AddOpenApi(options => options.Document.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Bookstore Ordering API";
        document.Info.Description = "Orders and the local copy of catalog data. Owned by the Ordering & Payment team.";
        return Task.CompletedTask;
    }));

WebApplication app = builder.Build();

if (DatabaseMigration.IsMigrationRun(args))
{
    await app.MigrateDatabaseAsync();
    return;
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapDefaultEndpoints();
app.MapOpenApi().WithDocumentPerVersion();
app.MapOrderEndpoints();

app.Run();
