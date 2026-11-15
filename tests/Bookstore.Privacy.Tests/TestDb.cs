using Bookstore.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Privacy.Tests;

public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public TestDb()
    {
        _connection.Open();
        using BookstoreContext db = CreateContext();
        db.Database.EnsureCreated();
    }

    public BookstoreContext CreateContext() =>
        new(new DbContextOptionsBuilder<BookstoreContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}

public sealed class FixedClock(DateTime utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
}
