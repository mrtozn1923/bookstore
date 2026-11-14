WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

WebApplication app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/", () => "Bookstore.Payments.Api");

app.Run();
