using Bookstore.Inventory;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddRabbitMQClient("messaging");

builder.Services.AddSingleton<StockLedger>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHostedService<OrderPlacedConsumer>();

builder.Build().Run();
