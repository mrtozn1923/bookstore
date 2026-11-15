using Bookstore.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// stdout belongs to the MCP protocol: all logs go to stderr.
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// The server does not connect to the database; it calls the read-only endpoints of the Bookstore API.
builder.Services.AddHttpClient<BookstoreApi>(client =>
    client.BaseAddress = new Uri(builder.Configuration["BOOKSTORE_API_URL"] ?? "http://localhost:5080"));

builder.Services
    .AddMcpServer(o => o.ServerInfo = new() { Name = "bookstore", Version = "1.0.0" })
    .WithStdioServerTransport()
    .WithTools<CatalogTools>();

await builder.Build().RunAsync();
