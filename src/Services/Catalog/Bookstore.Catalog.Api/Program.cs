using Asp.Versioning;
using Bookstore.Catalog.Books;
using Bookstore.Catalog.Data;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddSqlServerDbContext<CatalogDbContext>("catalogdb");
builder.AddRedisOutputCache("redis");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddOutputCache(CachePolicies.Configure);
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
        document.Info.Title = "Bookstore Catalog API";
        document.Info.Description = "Books, authors, categories and prices. Owned by the Catalog & Search team.";
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
app.UseOutputCache();

app.MapDefaultEndpoints();
app.MapOpenApi().WithDocumentPerVersion();
app.MapBookEndpoints();

app.Run();
