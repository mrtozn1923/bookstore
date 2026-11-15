using Bookstore.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

[assembly: AssemblyFixture(typeof(Bookstore.Api.Tests.BookstoreApiFactory))]

namespace Bookstore.Api.Tests;

// One API host and one fresh SQLite database file for the whole test run.
public sealed class BookstoreApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"bookstore-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:BookstoreDb", $"Data Source={databasePath}");
        builder.UseSetting("Database:MigrateOnStartup", "false");
        builder.ConfigureTestServices(services => services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { }));
    }

    public async ValueTask InitializeAsync()
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        BookstoreContext db = scope.ServiceProvider.GetRequiredService<BookstoreContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        File.Delete(databasePath);
    }
}
