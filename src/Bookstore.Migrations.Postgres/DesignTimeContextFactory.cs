using Bookstore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bookstore.Migrations.Postgres;

// Used by `dotnet ef` and by the migration bundle: no web host is started.
// The bundle's --connection argument overrides this connection string at run time.
public sealed class DesignTimeContextFactory : IDesignTimeDbContextFactory<BookstoreContext>
{
    public BookstoreContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__BookstoreDb")
            ?? "Host=localhost;Database=BookstoreDb;Username=bookstore";
        DbContextOptions<BookstoreContext> options = new DbContextOptionsBuilder<BookstoreContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(DesignTimeContextFactory).Assembly.GetName().Name))
            .Options;
        return new BookstoreContext(options);
    }
}
