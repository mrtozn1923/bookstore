using Asp.Versioning;
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
