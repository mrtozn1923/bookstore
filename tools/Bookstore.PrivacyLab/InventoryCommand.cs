using Bookstore.Data;
using Bookstore.Privacy;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.PrivacyLab;

public static class InventoryCommand
{
    public static void Run(bool sql)
    {
        // Only the model is needed; no database is opened.
        using var db = new BookstoreContext(new DbContextOptionsBuilder<BookstoreContext>()
            .UseSqlite("Data Source=:memory:").Options);
        IReadOnlyList<InventoryEntry> inventory = PersonalDataInventory.Build(db);

        if (sql)
        {
            Console.Write(PersonalDataInventory.ToSensitivityClassificationSql(inventory));
            return;
        }

        foreach (InventoryEntry e in inventory)
        {
            ProcessingActivity a = e.Activity;
            Console.WriteLine($"{e.Table} [{a.Category}] purpose: {a.Purpose}; basis: {a.LegalBasis}");
            Console.WriteLine($"  retention: {a.Retention}; roles: {string.Join(", ", a.Roles)}; copies: {string.Join(", ", a.Copies)}");
            foreach (InventoryColumn c in e.Columns)
            {
                Console.WriteLine($"  {c.Column,-20} {c.Classification,-14} {c.InformationType}");
            }
        }
    }
}
