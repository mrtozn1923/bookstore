using Bookstore.OrderImport.Worker;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory // appsettings.json next to the binary
});

builder.Logging.AddSimpleConsole(o =>
{
    o.SingleLine = true;
    o.TimestampFormat = "HH:mm:ss.fff ";
});

builder.Services.Configure<OrderImportOptions>(builder.Configuration.GetSection("OrderImport"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<OrderFileImporter>();
builder.Services.AddHostedService<OrderFileWatcher>();

builder.Build().Run();
