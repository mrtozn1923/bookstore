using Bookstore.Ordering.Api.Orders;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRabbitMQClient("messaging");

builder.Services.AddHttpClient<CatalogClient>(client => client.BaseAddress = new Uri("https+http://catalog"));
builder.Services.AddSingleton<OrderStore>();
builder.Services.AddSingleton<OrderingTelemetry>();
builder.Services.AddSingleton<OrderEventPublisher>();
builder.Services.AddHostedService<StockReservedConsumer>();
builder.Services.AddSingleton(TimeProvider.System);

WebApplication app = builder.Build();

app.MapDefaultEndpoints();
app.MapOrderEndpoints();

app.Run();
