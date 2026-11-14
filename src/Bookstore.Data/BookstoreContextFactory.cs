using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bookstore.Data;

// Used only by dotnet-ef at design time; the application uses AddDbContext.
public class BookstoreContextFactory : IDesignTimeDbContextFactory<BookstoreContext>
{
    public BookstoreContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Bookstore")
            ?? "Server=localhost;Database=BookstoreDb;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<BookstoreContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new BookstoreContext(options);
    }
}
