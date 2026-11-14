using System.Security.Cryptography;
using Bookstore.Crypto.Fields;
using Bookstore.Customers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Crypto.Tests;

public sealed class CustomerEncryptionTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly FieldEncryptor _encryptor = new(new FieldKeyRing(1, new Dictionary<byte, byte[]> { [1] = RandomNumberGenerator.GetBytes(32) }));
    private readonly BlindIndex _index = new(RandomNumberGenerator.GetBytes(32));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await _connection.OpenAsync(Ct);
        await using BookstoreContext db = NewContext();
        await db.Database.EnsureCreatedAsync(Ct);
    }

    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();

    private BookstoreContext NewContext() =>
        new(new DbContextOptionsBuilder<BookstoreContext>().UseSqlite(_connection).Options, _encryptor);

    private async Task<(Customer Ada, Customer Alan)> SeedAsync()
    {
        await using BookstoreContext db = NewContext();
        Customer ada = new() { Name = "Ada" };
        ada.SetPhone("+90 555 010 00 01", _index);
        Customer alan = new() { Name = "Alan" };
        alan.SetPhone("+90 555 010 00 02", _index);
        db.Customers.AddRange(ada, alan);
        await db.SaveChangesAsync(Ct);
        return (ada, alan);
    }

    [Fact]
    public async Task Phone_is_not_stored_in_plaintext_but_is_found_by_its_blind_index()
    {
        (Customer ada, _) = await SeedAsync();
        await using BookstoreContext db = NewContext();

        string stored = await db.Database.SqlQuery<string>($"SELECT Phone AS Value FROM Customers WHERE Id = {ada.Id}").SingleAsync(Ct);
        Assert.DoesNotContain("5550100001", stored);

        string wanted = _index.Compute(PhoneNumber.Normalize("0090 (555) 010 00 01"));
        Customer found = await db.Customers.SingleAsync(c => c.PhoneIndex == wanted, Ct);
        Assert.Equal("Ada", found.Name);
        Assert.Equal("+905550100001", found.Phone);
    }

    [Fact]
    public async Task Converter_binds_to_the_column_not_the_row()
    {
        (Customer ada, Customer alan) = await SeedAsync();
        await using BookstoreContext db = NewContext();

        // Someone with write access to the database copies Alan's ciphertext into Ada's row
        await db.Database.ExecuteSqlAsync(
            $"UPDATE Customers SET Phone = (SELECT Phone FROM Customers WHERE Id = {alan.Id}) WHERE Id = {ada.Id}", Ct);

        Customer reloaded = await db.Customers.SingleAsync(c => c.Id == ada.Id, Ct);
        Assert.Equal("+905550100002", reloaded.Phone);   // decrypts without error: the known limit
    }
}
