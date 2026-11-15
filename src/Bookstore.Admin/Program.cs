using System.CommandLine;
using Bookstore.Admin;
using Bookstore.Admin.Pricing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// System.CommandLine owns args; the host only provides DI, configuration and logging.
HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    ContentRootPath = AppContext.BaseDirectory,
});
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services.Configure<ImportOptions>(builder.Configuration.GetSection("Bookstore:Import"));
builder.Services.AddSingleton<IPriceCatalog, SimulatedPriceCatalog>();
builder.Services.AddSingleton<PriceImporter>();

using IHost host = builder.Build();
RootCommand root = AdminCommands.Create(host.Services);
return await root.Parse(args).InvokeAsync();
