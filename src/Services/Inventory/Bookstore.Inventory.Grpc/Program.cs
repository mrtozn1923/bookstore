WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

WebApplication app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/", () => "Bookstore.Inventory.Grpc");

app.Run();
