using Bookstore.Catalog.Data;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddSqlServerDbContext<CatalogDbContext>("catalogdb");

WebApplication app = builder.Build();

if (DatabaseMigration.IsMigrationRun(args))
{
    await app.MigrateDatabaseAsync();
    return;
}

app.MapDefaultEndpoints();
app.MapGet("/", () => "Bookstore.Catalog.Api");

app.Run();
