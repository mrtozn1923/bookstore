using Bookstore.Data;
using Bookstore.Payments;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

[assembly: AssemblyFixture(typeof(Bookstore.Api.Tests.BookstoreApiFactory))]

namespace Bookstore.Api.Tests;

// One API host and one fresh database for the whole test run.
// The database is the development one with a "_Tests" suffix: BookstoreDb → BookstoreDb_Tests.
public sealed class BookstoreApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string connectionString = TestConnectionString();

    // Tests never call the real provider; a test can make the next authorizations fail.
    public FakePaymentGateway Payments { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:BookstoreDb", connectionString);
        builder.UseSetting("Database:MigrateOnStartup", "false");
        builder.ConfigureServices(services => services.Replace(ServiceDescriptor.Singleton<IPaymentGateway>(Payments)));
    }

    public async ValueTask InitializeAsync()
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        BookstoreContext db = scope.ServiceProvider.GetRequiredService<BookstoreContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    private static string TestConnectionString()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables()
            .Build();
        string baseConnection = configuration.GetConnectionString("BookstoreDb") is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException("Connection string 'BookstoreDb' is missing (see README.md).");

        SqlConnectionStringBuilder csb = new(baseConnection);
        csb.InitialCatalog += "_Tests";
        return csb.ConnectionString;
    }
}

public sealed class FakePaymentGateway : IPaymentGateway
{
    private int failuresLeft;

    public void FailNext(int count) => Interlocked.Exchange(ref failuresLeft, count);

    public Task<PaymentResult> AuthorizeAsync(PaymentRequest request, CancellationToken ct) =>
        Interlocked.Decrement(ref failuresLeft) >= 0
            ? throw new HttpRequestException("Provider answered 503.")
            : Task.FromResult(new PaymentResult($"ch_{request.OrderId}", "authorized"));
}
