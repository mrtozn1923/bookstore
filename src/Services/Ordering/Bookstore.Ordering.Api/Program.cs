using System.Reflection;
using Asp.Versioning;
using Bookstore.Ordering;
using Bookstore.Ordering.CatalogCopy;
using Bookstore.Ordering.Data;
using Bookstore.Ordering.Orders;
using Bookstore.Contracts;
using Bookstore.Contracts.Inventory;
using Bookstore.Contracts.Ordering;
using Bookstore.Contracts.Payments;
using Bookstore.Ordering.Checkout;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.RabbitMQ;
using Wolverine.SqlServer;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
// The build-time OpenAPI generator (GetDocument.Insider) starts this host without Aspire's connection strings:
// no broker and no message store in that run.
bool openApiBuild = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
if (openApiBuild)
{
    builder.Services.DisableAllExternalWolverineTransports();
    builder.Services.DisableAllWolverineMessagePersistence();
    // The saga makes Wolverine build OrderingDbContext at startup. Nothing connects in this run,
    // but Aspire's AddSqlServerDbContext refuses an empty connection string.
    builder.Configuration["ConnectionStrings:orderingdb"] = "Server=openapi-build;Database=orderingdb";
}

// EF Core's retrying execution strategy (on by default in Aspire) does not allow the explicit transaction
// the outbox needs, so the service turns it off and leaves retries to Wolverine's error policies.
// The migrator keeps it: right after a container start, the first connection attempts may fail.
builder.AddSqlServerDbContext<OrderingDbContext>("orderingdb", settings => settings.DisableRetry = !DatabaseMigration.IsMigrationRun(args));

builder.UseWolverine(opts =>
{
    if (!openApiBuild)
    {
        // Inbox and outbox tables in Ordering's own database.
        opts.PersistMessagesWithSqlServerUsingNamedConnection("orderingdb", "wolverine");
        opts.UseRabbitMqUsingNamedConnection("rabbitmq")
            .AutoProvision()
            // Ordering owns this queue and its binding to Catalog's exchange.
            .DeclareExchange(Exchanges.Catalog, exchange => exchange.BindQueue(Queues.CatalogEvents))
            // The saga's replies come from Inventory's and Payments' exchanges into one Ordering queue.
            .DeclareExchange(Exchanges.Inventory, exchange => exchange.BindQueue(Queues.Checkout))
            .DeclareExchange(Exchanges.Payments, exchange => exchange.BindQueue(Queues.Checkout));
    }
    opts.UseEntityFrameworkCoreTransactions();
    // A handler that takes OrderingDbContext gets SaveChanges + outbox flush in one transaction.
    opts.Policies.AutoApplyTransactions();

    // Durable inbox: the message is stored in orderingdb before it is acknowledged to RabbitMQ.
    // Poison messages go to Ordering's own dead letter queue, not to the default one every service shares.
    opts.ListenToRabbitQueue(Queues.CatalogEvents)
        .UseDurableInbox()
        .DeadLetterQueueing(new DeadLetterQueue("ordering.dead-letter"));
    opts.ListenToRabbitQueue(Queues.Checkout)
        .UseDurableInbox()
        .DeadLetterQueueing(new DeadLetterQueue("ordering.dead-letter"));

    // Events go to Ordering's exchange.
    opts.PublishMessage<OrderPlaced>().ToRabbitExchange(Exchanges.Ordering);
    opts.PublishMessage<OrderConfirmed>().ToRabbitExchange(Exchanges.Ordering);
    opts.PublishMessage<OrderCancelled>().ToRabbitExchange(Exchanges.Ordering);
    // Commands go to the receiver's queue. Inventory and Payments own those queues (and their dead letter
    // settings): Ordering sends to them but never declares them.
    opts.PublishMessage<ReserveStock>().ToRabbitQueue(CommandQueues.Inventory).ExternallyOwned();
    opts.PublishMessage<CommitStock>().ToRabbitQueue(CommandQueues.Inventory).ExternallyOwned();
    opts.PublishMessage<ReleaseStock>().ToRabbitQueue(CommandQueues.Inventory).ExternallyOwned();
    opts.PublishMessage<AuthorizePayment>().ToRabbitQueue(CommandQueues.Payments).ExternallyOwned();
    opts.PublishMessage<RefundPayment>().ToRabbitQueue(CommandQueues.Payments).ExternallyOwned();
    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
    // StartCheckout and the scheduled CheckoutTimedOut stay in Ordering, persisted in orderingdb.
    opts.Policies.UseDurableLocalQueues();

    // Transient database errors: three retries with growing pauses, then the dead letter queue.
    opts.OnException<SqlException>().RetryWithCooldown(
        TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
    // Two messages for the same checkout at once (a late payment racing the timeout): the saga's Version check
    // rejects the second save. Retrying reloads the saga and applies the message to the newer state.
    opts.OnException<DbUpdateConcurrencyException>().Or<SagaConcurrencyException>()
        .RetryWithCooldown(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(500));
    // A message that can never succeed goes straight to the dead letter queue; retrying it only adds load.
    opts.OnException<InvalidOperationException>().MoveToErrorQueue();
});

// Every order endpoint needs a customer's access token for the bookstore-api audience. The BFF forwards it.
builder.Services.AddAuthentication()
    .AddKeycloakJwtBearer("keycloak", realm: "bookstore", options =>
    {
        options.Audience = "bookstore-api";
        options.MapInboundClaims = false;
        options.TokenValidationParameters.RoleClaimType = "roles";
        options.RequireHttpsMetadata = builder.Environment.IsProduction();
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("customer", policy => policy.RequireRole("customer"));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<PlaceOrderHandler>();
builder.Services.AddSingleton<CheckoutMetrics>();
builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
builder.Services.Configure<CheckoutOptions>(builder.Configuration.GetSection("Checkout"));
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
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapOpenApi().WithDocumentPerVersion();
app.MapOrderEndpoints();

app.Run();
