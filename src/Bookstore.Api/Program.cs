var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Host.ConfigureHostOptions(options => options.ShutdownTimeout = TimeSpan.FromSeconds(15));

string catalogName = builder.Configuration["Catalog:Name"] ?? "Bookstore Catalog";

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.Logger.LogInformation("Development mode: detailed error pages are on");
}

app.MapControllers();

app.MapGet("/info", (IWebHostEnvironment env) => new
{
    Catalog = catalogName,
    env.ApplicationName,
    env.EnvironmentName,
    PageSize = app.Configuration.GetValue<int>("Catalog:PageSize")
});

app.Lifetime.ApplicationStarted.Register(() => app.Logger.LogInformation("{Catalog} is ready", catalogName));

app.Run();
