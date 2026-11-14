using Bookstore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.Api.Tests;

public sealed class DatabaseResilienceTests(BookstoreApiFactory factory)
{
    [Fact]
    public void BookstoreDb_retries_transient_sql_errors()
    {
        using IServiceScope scope = factory.Services.CreateScope();
        BookstoreContext db = scope.ServiceProvider.GetRequiredService<BookstoreContext>();

        Assert.IsType<SqlServerRetryingExecutionStrategy>(db.Database.CreateExecutionStrategy());
    }
}
