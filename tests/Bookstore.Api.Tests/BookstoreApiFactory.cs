using Bookstore.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: AssemblyFixture(typeof(Bookstore.Api.Tests.BookstoreApiFactory))]

namespace Bookstore.Api.Tests;

// One API host and one fresh database for the whole test run.
// The database is the development one with a "_Tests" suffix: BookstoreDb → BookstoreDb_Tests.
public sealed class BookstoreApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string connectionString = TestConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:BookstoreDb", connectionString);
        builder.UseSetting("Database:MigrateOnStartup", "false");
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
