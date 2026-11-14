using System.Reflection;
using Asp.Versioning;
using Bookstore.Inventory.V1;
using Bookstore.Ordering.CatalogCopy;
using Bookstore.Ordering.Data;
using Bookstore.Ordering.Orders;
using Bookstore.Contracts;
using Bookstore.Contracts.Ordering;
using Microsoft.Data.SqlClient;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.RabbitMQ;
using Wolverine.SqlServer;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
// EF Core's retrying execution strategy (on by default in Aspire) does not allow the explicit transaction
// the outbox needs, so the service turns it off and leaves retries to Wolverine's error policies.
// The migrator keeps it: right after a container start, the first connection attempts may fail.
builder.AddSqlServerDbContext<OrderingDbContext>("orderingdb", settings => settings.DisableRetry = !DatabaseMigration.IsMigrationRun(args));

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
        // Inbox and outbox tables in Ordering's own database.
        opts.PersistMessagesWithSqlServerUsingNamedConnection("orderingdb", "wolverine");
        opts.UseRabbitMqUsingNamedConnection("rabbitmq")
            .AutoProvision()
            // Ordering owns this queue and its binding to Catalog's exchange.
            .DeclareExchange(Exchanges.Catalog, exchange => exchange.BindQueue(Queues.CatalogEvents));
    }
    opts.UseEntityFrameworkCoreTransactions();
    // A handler that takes OrderingDbContext gets SaveChanges + outbox flush in one transaction.
    opts.Policies.AutoApplyTransactions();

    // Durable inbox: the message is stored in orderingdb before it is acknowledged to RabbitMQ.
    // Poison messages go to Ordering's own dead letter queue, not to the default one every service shares.
    opts.ListenToRabbitQueue(Queues.CatalogEvents)
        .UseDurableInbox()
        .DeadLetterQueueing(new DeadLetterQueue("ordering.dead-letter"));

    opts.PublishMessage<OrderPlaced>().ToRabbitExchange(Exchanges.Ordering);
    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();

    // Transient database errors: three retries with growing pauses, then the dead letter queue.
    opts.OnException<SqlException>().RetryWithCooldown(
        TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
    // A message that can never succeed goes straight to the dead letter queue; retrying it only adds load.
    opts.OnException<InvalidOperationException>().MoveToErrorQueue();
});

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
