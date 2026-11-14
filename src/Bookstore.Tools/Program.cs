using Bookstore.Tools;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});
builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(8));
builder.Services.Configure<ImportOptions>(builder.Configuration.GetSection(ImportOptions.SectionName));
builder.Services.AddHostedService<ImportWorker>();

using IHost host = builder.Build();
await host.RunAsync();
