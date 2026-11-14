using Bookstore.Inventory;

var builder = WebApplication.CreateBuilder(args);

// Artificial delay to simulate a slow Inventory in aggregation demos.
int responseDelayMs = builder.Configuration.GetValue<int>("Inventory:ResponseDelayMs");
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");

Warehouse[] warehouses =
[
    new("ist-1", "Istanbul"),
    new("ank-1", "Ankara"),
];

StockItem[] items =
[
    new("9780441172719", "ist-1", 12, 2),
    new("9780441172719", "ank-1", 3, 0),
    new("9780141439587", "ist-1", 4, 4),
    new("9780547928227", "ank-1", 7, 1),
];

app.MapGet("/stock/{isbn}", async (string isbn, CancellationToken ct) =>
{
    if (responseDelayMs > 0)
    {
        await Task.Delay(responseDelayMs, ct);
    }

    WarehouseStock[] perWarehouse =
    [
        .. items
            .Where(i => i.Isbn == isbn)
            .Join(warehouses, i => i.WarehouseId, w => w.Id, (i, w) => new WarehouseStock(w.Id, w.City, i.Available)),
    ];

    return perWarehouse.Length == 0
        ? Results.NotFound()
        : Results.Ok(new StockLevel(isbn, perWarehouse.Sum(w => w.Available), perWarehouse));
});

app.Run();
