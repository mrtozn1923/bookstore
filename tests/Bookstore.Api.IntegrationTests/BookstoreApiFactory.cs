using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Bookstore.Api.Data;

namespace Bookstore.Api.IntegrationTests;

public class BookstoreApiFactory : WebApplicationFactory<Program>
{
    // the in-memory database lives as long as the connection stays open
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Testing doesn't read user secrets; without a key every request returns 500
        builder.UseSetting("Jwt:SigningKey", "integration-tests-only-signing-key-0123456789");

        builder.ConfigureServices(services =>
        {
            // find the registration from Program.cs and remove it
            var descriptor = services.Single(
                d => d.ServiceType == typeof(DbContextOptions<BookstoreContext>));
            services.Remove(descriptor);

            _connection.Open();
            services.AddDbContext<BookstoreContext>(options => options.UseSqlite(_connection));

            // replace Redis with an in-memory cache as well
            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // build the schema; the seed data (Dune, Emma, The Hobbit) comes from HasData
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookstoreContext>();
        db.Database.EnsureCreated();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }
}
