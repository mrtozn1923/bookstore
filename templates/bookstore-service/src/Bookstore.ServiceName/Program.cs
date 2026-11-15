WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
#if (HealthChecks)
builder.Services.AddHealthChecks();
#endif

WebApplication app = builder.Build();

app.MapGet("/", () => "Bookstore.ServiceName");
#if (HealthChecks)
app.MapHealthChecks("/health");
#endif

app.Run();
