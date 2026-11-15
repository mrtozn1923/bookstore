using Bookstore.Api.Data;
using Bookstore.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Respawn;
using WireMock.Server;

namespace Bookstore.Api.IntegrationTests;

public sealed class BookstoreApiFactory(SqlServerFixture sql, MailpitFixture mail) : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string connectionString = sql.ConnectionStringFor($"Bookstore_{Guid.NewGuid():N}");
    private Respawner? respawner;

    public WireMockServer PaymentApi { get; } = WireMockServer.Start();

    public WireMockServer ShippingApi { get; } = WireMockServer.Start();

    public string ConnectionString => connectionString;

    public async ValueTask InitializeAsync()
    {
        DbContextOptions<BookstoreDbContext> options = new DbContextOptionsBuilder<BookstoreDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        await using (BookstoreDbContext db = new(options))
        {
            await db.Database.MigrateAsync();
        }

        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            TablesToIgnore = ["__EFMigrationsHistory"]
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await respawner!.ResetAsync(connection);
        PaymentApi.Reset();
        ShippingApi.Reset();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Bookstore", connectionString);
        builder.UseSetting("Payments:BaseUrl", $"{PaymentApi.Url}/");
        builder.UseSetting("Payments:ApiKey", "test-key");
        builder.UseSetting("Payments:Resilience:Retry:Delay", "00:00:00.050");
        builder.UseSetting("Payments:Resilience:AttemptTimeout:Timeout", "00:00:00.500");
        builder.UseSetting("Shipping:BaseUrl", $"{ShippingApi.Url}/");
        builder.UseSetting("Smtp:Host", mail.SmtpHost);
        builder.UseSetting("Smtp:Port", mail.SmtpMappedPort.ToString());

        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            ServiceDescriptor worker = services.Single(d =>
                d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(LowStockReportService));
            services.Remove(worker);
        });
    }

    public HttpClient CreateClientFor(TestUser user, bool allowAutoRedirect = true)
    {
        HttpClient client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = allowAutoRedirect });
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, user.HeaderValue);
        return client;
    }

    public override async ValueTask DisposeAsync()
    {
        PaymentApi.Stop();
        ShippingApi.Stop();
        await base.DisposeAsync();
    }
}
