using Microsoft.EntityFrameworkCore;

namespace Bookstore.Institutional.ReferenceData;

public static class LookupCheck
{
    // At startup: do the enum and the table say the same thing?
    public static async Task<bool> VerifyAsync(BookstoreContext db, TextWriter log)
    {
        var rows = await db.OrderStatuses.AsNoTracking().ToDictionaryAsync(r => (int)r.Id, r => r.Code);
        var code = Enum.GetValues<OrderStatus>().ToDictionary(v => (int)v, v => v.ToString());

        var problems = code.Where(c => !rows.TryGetValue(c.Key, out var dbCode) || dbCode != c.Value)
            .Select(c => $"enum {c.Key}={c.Value} has no matching row")
            .Concat(rows.Where(r => !code.ContainsKey(r.Key)).Select(r => $"row {r.Key}={r.Value} is unknown to the code"))
            .ToList();

        foreach (string p in problems)
            log.WriteLine($"OrderStatus mismatch: {p}");
        log.WriteLine(problems.Count == 0 ? "OrderStatus lookup is in sync." : $"{problems.Count} mismatch(es).");
        return problems.Count == 0;
    }
}
