using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Bookstore.Catalog.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PactNet;
using PactNet.Infrastructure.Outputters;
using PactNet.Verifier;
using Testcontainers.MsSql;
using Testcontainers.Redis;
using Wolverine;

namespace Bookstore.Catalog.ContractTests;

// Provider side: the real Catalog, on a real port, answers every interaction in the BFF's pact.
public sealed class CatalogProviderPactTests : IAsyncLifetime
{
    private readonly MsSqlContainer sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest")
        .WithName($"msapp-test-sql-{Guid.NewGuid():N}"[..24])
        .WithEnvironment("MSSQL_MEMORY_LIMIT_MB", "1024")
        .Build();
    private readonly RedisContainer redis = new RedisBuilder("redis:8.6").WithName($"msapp-test-redis-{Guid.NewGuid():N}"[..26]).Build();
    private WebApplicationFactory<Program> catalog = null!;

    // Pact's native library reports anonymous usage statistics unless this is set
    static CatalogProviderPactTests() => Environment.SetEnvironmentVariable("PACT_DO_NOT_TRACK", "true");
    private Uri baseAddress = null!;

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(sql.StartAsync(), redis.StartAsync());
        string catalogDb = new SqlConnectionStringBuilder(sql.GetConnectionString()) { InitialCatalog = "catalogdb" }.ConnectionString;
        await using (CatalogDbContext db = new(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlServer(catalogDb).Options))
        {
            await db.Database.MigrateAsync();
        }

        catalog = new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
        {
            web.UseSetting("ConnectionStrings:catalogdb", catalogDb);
            web.UseSetting("ConnectionStrings:redis", redis.GetConnectionString());
            // Events are not part of this contract: Wolverine keeps its outbox in SQL but talks to no broker
            web.UseSetting("ConnectionStrings:rabbitmq", "amqp://localhost:1");
            web.ConfigureTestServices(services =>
            {
                services.DisableAllExternalWolverineTransports();
                services.AddSingleton<IStartupFilter>(new ProviderStates(catalogDb));
            });
        });
        // A real port: the Pact verifier is a separate (native) process and cannot use the in-memory TestServer
        catalog.UseKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, 0));
        catalog.StartServer();
        baseAddress = new Uri(catalog.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First());
    }

    [Fact]
    public void Catalog_honours_the_BFF_pact()
    {
        using PactVerifier verifier = new("Bookstore.Catalog", new PactVerifierConfig { Outputters = [new TestOutput()] });

        verifier.WithHttpEndpoint(baseAddress)
            .WithFileSource(new FileInfo(Path.Combine(PactFolder(), "Bookstore.Bff-Bookstore.Catalog.json")))
            .WithProviderStateUrl(new Uri(baseAddress, "/provider-states"))
            .Verify();
    }

    public async ValueTask DisposeAsync()
    {
        await catalog.DisposeAsync();
        await Task.WhenAll(sql.DisposeAsync().AsTask(), redis.DisposeAsync().AsTask());
    }

    private static string PactFolder([CallerFilePath] string path = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "pacts"));

    // Pact calls this endpoint before each interaction with the interaction's "given" text
    private sealed class ProviderStates(string catalogDb) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Map("/provider-states", states => states.Run(async context =>
            {
                string state = (await JsonDocument.ParseAsync(context.Request.Body)).RootElement.GetProperty("state").GetString() ?? "";
                await using CatalogDbContext db = new(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlServer(catalogDb).Options);
                if (state == "the book 9780141439587 exists" && !await db.Books.AnyAsync())
                {
                    await CatalogSeed.SeedAsync(db, context.RequestAborted);
                }
                // "the book 9780000000000 does not exist": the sample data never has it
                context.Response.StatusCode = StatusCodes.Status200OK;
            }));
            next(app);
        };
    }

    private sealed class TestOutput : IOutput
    {
        public void WriteLine(string line) => TestContext.Current.TestOutputHelper?.WriteLine(line);
    }
}
