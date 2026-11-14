using Bookstore.Institutional;
using Microsoft.EntityFrameworkCore;

namespace Cd02.VirtualRoot;

public static class Seed
{
    public static async Task ResetAsync(BookstoreContext db)
    {
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        db.Institutions.Add(new Institution { Name = "Sample School", TaxNumber = "1234567890" });
        await db.SaveChangesAsync();

        // Ids 1-8 in this order: three top-level units, then their children
        (int? Parent, string Code, string Name)[] units =
        [
            (null, "ADM", "Administration"), (null, "LIB", "Library"), (null, "HS", "High School"),
            (1, "ACC", "Accounting"), (1, "PUR", "Purchasing"), (2, "ARC", "Archive"),
            (3, "SCI", "Science"), (3, "LIT", "Literature")
        ];
        foreach (var u in units)
        {
            db.InstitutionUnits.Add(new InstitutionUnit { InstitutionId = 1, ParentId = u.Parent, Code = u.Code, Name = u.Name });
            await db.SaveChangesAsync();
        }

        (int Unit, string Name, string Email)[] users =
        [
            (7, "Ada Lovelace", "ada@example.com"), (7, "Alan Turing", "alan@example.com"),
            (4, "Grace Hopper", "grace@example.com"), (5, "Linus Torvalds", "linus@example.com"),
            (2, "Barbara Liskov", "barbara@example.com"), (6, "Margaret Hamilton", "margaret@example.com"),
            (8, "Donald Knuth", "donald@example.com"), (1, "Ken Thompson", "ken@example.com")
        ];
        foreach (var u in users)
            db.InstitutionUsers.Add(new InstitutionUser { InstitutionUnitId = u.Unit, FullName = u.Name, Email = u.Email, JoinedOn = new DateOnly(2025, 9, 1) });
        await db.SaveChangesAsync();
    }
}
