using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bookstore.Data;

// Used only by dotnet ef; the connection string is never opened when adding a migration
public sealed class DesignTimeContextFactory : IDesignTimeDbContextFactory<BookstoreContext>
{
    public BookstoreContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<BookstoreContext>()
            .UseSqlServer("Server=localhost;Database=BookstoreDb;Integrated Security=true")
            .Options);
}
