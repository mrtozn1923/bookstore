using System.Reflection;
using Bookstore.Contracts;
using Bookstore.Contracts.Inventory;
using Bookstore.Inventory.Data;
using Bookstore.Inventory.Stock;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.RabbitMQ;
using Wolverine.SqlServer;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
// Same reasoning as Catalog and Ordering: the outbox needs its own transaction, the migrator keeps EF Core's retries.
builder.AddSqlServerDbContext<InventoryDbContext>("inventorydb", settings => settings.DisableRetry = !DatabaseMigration.IsMigrationRun(args));

builder.UseWolverine(opts =>
{
    opts.PersistMessagesWithSqlServerUsingNamedConnection("inventorydb", "wolverine");
    opts.UseRabbitMqUsingNamedConnection("rabbitmq").AutoProvision();
    opts.UseEntityFrameworkCoreTransactions();
    opts.Policies.AutoApplyTransactions();

    // Inventory owns its command queue; the checkout saga only knows its name.
    opts.ListenToRabbitQueue(CommandQueues.Inventory)
        .UseDurableInbox()
        .DeadLetterQueueing(new DeadLetterQueue("inventory.dead-letter"));

    opts.PublishMessage<StockReserved>().ToRabbitExchange(Exchanges.Inventory);
    opts.PublishMessage<StockReservationFailed>().ToRabbitExchange(Exchanges.Inventory);
    opts.PublishMessage<StockCommitted>().ToRabbitExchange(Exchanges.Inventory);
    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();

    // The reservation retries five times itself; if the row is still contended, the whole message is tried again later.
    opts.OnException<DbUpdateConcurrencyException>().RetryWithCooldown(
        TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(2));
    opts.OnException<InvalidOperationException>().MoveToErrorQueue();
});

// gRPC callers are services, not users: a client credentials token for the inventory-api audience.
builder.Services.AddAuthentication()
    .AddKeycloakJwtBearer("keycloak", realm: "bookstore", options =>
    {
        options.Audience = "inventory-api";
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = builder.Environment.IsProduction();
    });
builder.Services.AddAuthorization();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<ReservationOptions>(builder.Configuration.GetSection("Reservations"));
builder.Services.AddScoped<StockReservations>();
builder.Services.AddHostedService<ReservationExpiryService>();
builder.Services.AddGrpc(options =>
{
    if (builder.Configuration.GetValue<TimeSpan>("Inventory:SimulatedLatency") > TimeSpan.Zero)
    {
        options.Interceptors.Add<SimulatedLatencyInterceptor>();
    }
});

WebApplication app = builder.Build();

if (DatabaseMigration.IsMigrationRun(args))
{
    await app.MigrateDatabaseAsync();
    return;
}

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapGrpcService<InventoryGrpcService>().RequireAuthorization();

app.Run();
