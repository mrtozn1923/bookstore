using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Bookstore.Institutional.Settings;

public sealed record SettingKey<T>(string Name);

public static class SettingKeys
{
    public static readonly SettingKey<int> InvoiceDueDays = new("invoice.due-days");
    public static readonly SettingKey<bool> EmailNotifications = new("notifications.email");
}

public sealed record GlobalSettings(Dictionary<string, SettingDefinition> Definitions, List<SettingValue> Values);

public sealed record ResolvedSetting(string Value, SettingScope Source);

public sealed class SettingsService(BookstoreContext db, IMemoryCache cache, TimeProvider clock)
{
    private const string GlobalKey = "settings:global";
    private static string InstitutionKey(int institutionId) => $"settings:institution:{institutionId}";

    public async Task<(T Value, SettingScope Source)> GetAsync<T>(
        SettingKey<T> key, int institutionId, int userId, CancellationToken ct = default)
    {
        GlobalSettings global = (await cache.GetOrCreateAsync(GlobalKey, async e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return new GlobalSettings(
                await db.SettingDefinitions.AsNoTracking().ToDictionaryAsync(d => d.Key, ct),
                await db.SettingValues.AsNoTracking().Where(v => v.Scope == SettingScope.Global).ToListAsync(ct));
        }))!;

        List<SettingValue> institutionValues = (await cache.GetOrCreateAsync(InstitutionKey(institutionId), async e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return await db.SettingValues.AsNoTracking().Where(v => v.InstitutionId == institutionId).ToListAsync(ct);
        }))!;

        // Precedence: user > institution > global > code default
        SettingValue? best =
            institutionValues.FirstOrDefault(v => v.Key == key.Name && v.UserId == userId)
            ?? institutionValues.FirstOrDefault(v => v.Key == key.Name && v.UserId == null)
            ?? global.Values.FirstOrDefault(v => v.Key == key.Name);

        ResolvedSetting r = best is not null
            ? new ResolvedSetting(best.Value, best.Scope)
            : new ResolvedSetting(global.Definitions[key.Name].DefaultValue, SettingScope.Default);
        return ((T)Convert.ChangeType(r.Value, typeof(T), CultureInfo.InvariantCulture), r.Source);
    }

    public async Task SetAsync<T>(SettingKey<T> key, T value, SettingScope scope,
        int? institutionId, int? userId, int changedBy, string reason, CancellationToken ct = default)
        where T : notnull
    {
        SettingDefinition def = await db.SettingDefinitions.SingleAsync(d => d.Key == key.Name, ct);
        if (scope > def.LowestScope)
            throw new InvalidOperationException($"Setting '{key.Name}' cannot be overridden at {scope} scope.");

        string text = Convert.ToString(value, CultureInfo.InvariantCulture)!;
        if (def.Type == SettingType.Bool) text = text.ToLowerInvariant();
        if (def.Type == SettingType.Int && value is int n && (n < def.Min || n > def.Max))
            throw new ArgumentOutOfRangeException(nameof(value), $"Value {n} is out of range [{def.Min}, {def.Max}].");

        SettingValue? row = await db.SettingValues.SingleOrDefaultAsync(
            v => v.Key == key.Name && v.InstitutionId == institutionId && v.UserId == userId, ct);
        string? old = row?.Value;
        if (row is null)
        {
            row = new SettingValue { Key = key.Name, Scope = scope, InstitutionId = institutionId, UserId = userId };
            db.SettingValues.Add(row);
        }
        row.Value = text;

        db.SettingChanges.Add(new SettingChange
        {
            Key = key.Name, Scope = scope, InstitutionId = institutionId, UserId = userId,
            OldValue = old, NewValue = text, ChangedBy = changedBy,
            ChangedAt = clock.GetUtcNow(), Reason = reason
        });
        await db.SaveChangesAsync(ct);

        cache.Remove(institutionId is int i ? InstitutionKey(i) : GlobalKey);
    }
}
