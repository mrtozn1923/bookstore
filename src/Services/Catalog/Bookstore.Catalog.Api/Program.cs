using System.Reflection;
using Asp.Versioning;
using Bookstore.Catalog.Books;
using Bookstore.Catalog.Data;
using Bookstore.Contracts;
using Bookstore.Contracts.Catalog;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.RabbitMQ;
using Wolverine.SqlServer;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
// EF Core's retrying execution strategy (on by default in Aspire) does not allow the explicit transaction
// the outbox needs, so the service turns it off and leaves retries to Wolverine's error policies.
// The migrator keeps it: right after a container start, the first connection attempts may fail.
builder.AddSqlServerDbContext<CatalogDbContext>("catalogdb", settings => settings.DisableRetry = !DatabaseMigration.IsMigrationRun(args));
builder.AddRedisOutputCache("redis");

// The build-time OpenAPI generator (GetDocument.Insider) starts this host without Aspire's connection strings:
// no broker and no message store in that run.
bool openApiBuild = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
if (openApiBuild)
{
    builder.Services.DisableAllExternalWolverineTransports();
    builder.Services.DisableAllWolverineMessagePersistence();
}

builder.UseWolverine(opts =>
{
    if (!openApiBuild)
    {
        // Outbox tables live in Catalog's own database, in the "wolverine" schema.
        opts.PersistMessagesWithSqlServerUsingNamedConnection("catalogdb", "wolverine");
        opts.UseRabbitMqUsingNamedConnection("rabbitmq").AutoProvision();
    }
    opts.UseEntityFrameworkCoreTransactions();

    opts.PublishMessage<BookPriceChanged>().ToRabbitExchange(Exchanges.Catalog);
    opts.PublishMessage<BookRenamed>().ToRabbitExchange(Exchanges.Catalog);

    // Nothing leaves the service without first being written to the outbox table.
    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
});

// Access tokens from the "bookstore" realm, for the bookstore-api audience. Reading the catalog stays anonymous;
// changing it needs the store-staff role.
builder.Services.AddAuthentication()
    .AddKeycloakJwtBearer("keycloak", realm: "bookstore", options =>
    {
        options.Audience = "bookstore-api";
        options.MapInboundClaims = false;
        options.TokenValidationParameters.RoleClaimType = "roles";
        // Keycloak runs on plain HTTP locally; production metadata must come over HTTPS.
        options.RequireHttpsMetadata = builder.Environment.IsProduction();
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("store-staff", policy => policy.RequireRole("store-staff"));

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
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

app.MapDefaultEndpoints();
app.MapOpenApi().WithDocumentPerVersion();
app.MapBookEndpoints();

app.Run();
