using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Institutional.Import;

public static class NaiveImport
{
    public static async Task<int> RunAsync(BookstoreContext db, Stream csv, int institutionId)
    {
        Dictionary<string, int> units = await db.InstitutionUnits
            .Where(u => u.InstitutionId == institutionId).ToDictionaryAsync(u => u.Code, u => u.Id);
        using StreamReader reader = new(csv);
        await reader.ReadLineAsync(); // header
        int row = 1;
        while (await reader.ReadLineAsync() is { } line)
        {
            row++;
            string[] f = line.Split(',');
            db.InstitutionUsers.Add(new InstitutionUser
            {
                InstitutionId = institutionId,
                StaffNumber = f[0],
                FullName = f[1].Trim('"'),
                Email = f[2],
                InstitutionUnitId = units[f[3]],
                JoinedOn = DateOnly.ParseExact(f[4], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                SpendingLimit = decimal.Parse(f[5], CultureInfo.InvariantCulture)
            });
            await db.SaveChangesAsync();
        }
        return row;
    }
}
