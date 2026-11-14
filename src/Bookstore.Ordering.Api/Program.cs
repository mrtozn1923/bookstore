using Bookstore.Ordering.Api.Orders;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient<CatalogClient>(client => client.BaseAddress = new Uri("https+http://catalog"));
builder.Services.AddSingleton<OrderStore>();
builder.Services.AddSingleton<OrderingTelemetry>();
builder.Services.AddSingleton(TimeProvider.System);

WebApplication app = builder.Build();

app.MapDefaultEndpoints();
app.MapOrderEndpoints();

app.Run();
