using System.Security.Cryptography;
using Bookstore.Data;
using Bookstore.Ordering;
using Bookstore.Privacy.Protection;
using Bookstore.TestData;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.PrivacyLab;

public static class ProtectDemo
{
    public static async Task RunAsync()
    {
        // Demo keys live only in memory for this run; production reads them from a secret store.
        var options = new PiiProtectionOptions
        {
            ActiveKeyId = "k1",
            EncryptionKeys = new Dictionary<string, byte[]> { ["k1"] = RandomNumberGenerator.GetBytes(32) },
            BlindIndexKey = RandomNumberGenerator.GetBytes(32),
        };
        var protector = new PiiProtector(options);

        File.Delete("protect-demo.db");
        await using var db = new BookstoreContext(new DbContextOptionsBuilder<BookstoreContext>()
            .UseSqlite("Data Source=protect-demo.db;Pooling=False").Options);
        await db.Database.EnsureCreatedAsync();
        var vault = new NationalIdVault(db, protector);

        List<(Customer Customer, string FakeNationalId)> people = SyntheticData.Customers(3);
        foreach ((Customer customer, string fakeId) in people)
        {
            var order = new Order { OrderedAtUtc = DateTime.UtcNow, Total = 245.00m, ShippingAddress = $"Example St. 1, {customer.City}", BillingName = customer.FullName };
            vault.SetBillingNationalId(order, fakeId);
            customer.Orders.Add(order);
            db.Customers.Add(customer);
            Console.WriteLine($"{customer.FullName,-16} {customer.Email,-30} fake id {fakeId} valid={NationalIdChecksum.IsValid(fakeId)}");
        }
        await db.SaveChangesAsync();

        // What a database reader (backup, replica, DBA) sees.
        await using (var connection = new SqliteConnection("Data Source=protect-demo.db;Pooling=False"))
        {
            await connection.OpenAsync();
            await using SqliteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, BillingNationalIdEncrypted, BillingNationalIdIndex FROM Orders ORDER BY Id LIMIT 1";
            await using SqliteDataReader r = await cmd.ExecuteReaderAsync();
            await r.ReadAsync();
            Console.WriteLine($"raw row: {r.GetInt32(0)} | {r.GetString(1)} | {r.GetString(2)}");
        }

        string searchFor = people[1].FakeNationalId;
        Console.WriteLine($"orders for {searchFor}: [{string.Join(", ", await vault.FindOrderIdsAsync(searchFor))}]");
        Console.WriteLine($"StoreStaff: {await vault.GetForRoleAsync(2, "StoreStaff")}");
        Console.WriteLine($"Accounting: {await vault.GetForRoleAsync(2, "Accounting")}");

        // The purpose is authenticated data: a value copied into another column does not decrypt.
        string ciphertext = (await db.Orders.AsNoTracking().FirstAsync()).BillingNationalIdEncrypted!;
        try
        {
            protector.Decrypt(ciphertext, "Customers.Phone");
        }
        catch (CryptographicException ex)
        {
            Console.WriteLine($"wrong purpose: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
