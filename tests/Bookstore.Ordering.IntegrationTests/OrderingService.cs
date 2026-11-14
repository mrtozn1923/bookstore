using System.Security.Claims;
using System.Text.Encodings.Web;
using Bookstore.Ordering.CatalogCopy;
using Bookstore.Ordering.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;

namespace Bookstore.Ordering.IntegrationTests;

// Ordering as a real process boundary: its own SQL Server and RabbitMQ in containers, the real Program.cs,
// only authentication replaced. One instance for the whole test class (the containers take ~15 s to start).
public sealed class OrderingService : IAsyncLifetime
{
    private readonly MsSqlContainer sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest")
        .WithName($"msapp-test-sql-{Guid.NewGuid():N}"[..24])
        .WithEnvironment("MSSQL_MEMORY_LIMIT_MB", "1024")
        .Build();

    private readonly RabbitMqContainer rabbitmq = new RabbitMqBuilder("rabbitmq:4.3-management")
        .WithName($"msapp-test-rabbitmq-{Guid.NewGuid():N}"[..29])
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public string RabbitMqUri => rabbitmq.GetConnectionString();
    public LogCollector Logs { get; } = new();

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(sql.StartAsync(), rabbitmq.StartAsync());
        string orderingDb = new SqlConnectionStringBuilder(sql.GetConnectionString()) { InitialCatalog = "orderingdb" }.ConnectionString;

        // Schema first (the migrator's job in Aspire): Wolverine creates its own tables in this database at startup.
        // Then the data Ordering needs: its copy of two books.
        await using (OrderingDbContext db = new(new DbContextOptionsBuilder<OrderingDbContext>().UseSqlServer(orderingDb).Options))
        {
            await db.Database.MigrateAsync();
            db.CatalogBooks.AddRange(
                new CatalogBook { Isbn = "9780141439587", Title = "Emma", Price = 249.90m, Currency = "TRY", PriceVersion = 1, TitleVersion = 1 },
                new CatalogBook { Isbn = "9780441172719", Title = "Dune", Price = 489m, Currency = "TRY", PriceVersion = 1, TitleVersion = 1 });
            await db.SaveChangesAsync();
        }

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
        {
            // The same names Aspire's WithReference gives the service
            web.UseSetting("ConnectionStrings:orderingdb", orderingDb);
            web.UseSetting("ConnectionStrings:rabbitmq", RabbitMqUri);
            web.ConfigureLogging(logging => logging.AddProvider(Logs));
            web.ConfigureTestServices(services =>
            {
                // No Keycloak in this test: every request is Ada, a customer
                services.AddAuthentication(TestUser.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestUser>(TestUser.SchemeName, _ => { });
            });
        });
    }

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
        await Task.WhenAll(sql.DisposeAsync().AsTask(), rabbitmq.DisposeAsync().AsTask());
    }
}

public sealed class TestUser(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string Subject = "test-subject-ada";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Claims as Keycloak issues them (MapInboundClaims = false in Ordering)
        ClaimsIdentity identity = new(
            [new Claim("sub", Subject), new Claim("name", "Ada Lovelace"), new Claim("email", "ada@example.com"), new Claim("roles", "customer")],
            SchemeName, "name", "roles");
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}

// Keeps the log lines the tests want to count (Wolverine's retries, for example)
public sealed class LogCollector : ILoggerProvider
{
    private readonly System.Collections.Concurrent.ConcurrentQueue<string> lines = new();
    public IReadOnlyCollection<string> Lines => lines;
    public ILogger CreateLogger(string categoryName) => new Logger(lines);
    public void Dispose() { }

    private sealed class Logger(System.Collections.Concurrent.ConcurrentQueue<string> lines) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            lines.Enqueue($"{logLevel}: {formatter(state, exception)} {exception?.GetType().Name}");
    }
}
