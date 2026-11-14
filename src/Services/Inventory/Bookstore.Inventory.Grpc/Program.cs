using Bookstore.Inventory.Data;
using Bookstore.Inventory.Stock;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddSqlServerDbContext<InventoryDbContext>("inventorydb");

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

app.MapDefaultEndpoints();
app.MapGrpcService<InventoryGrpcService>();

app.Run();
