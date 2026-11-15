using System.Text.Json;

namespace Bookstore.CampaignController;

// Stands in for a system outside the cluster (CDN, search index, payment provider):
// the controller publishes campaign banners here and must remove them before the Campaign disappears.
public sealed class StorefrontCache(string path)
{
    public async Task PublishAsync(string key, string title, CancellationToken ct)
    {
        Dictionary<string, string> banners = await LoadAsync(ct);
        banners[key] = title;
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(banners), ct);
    }

    public async Task RemoveAsync(string key, CancellationToken ct)
    {
        Dictionary<string, string> banners = await LoadAsync(ct);
        banners.Remove(key);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(banners), ct);
    }

    private async Task<Dictionary<string, string>> LoadAsync(CancellationToken ct) =>
        File.Exists(path)
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync(path, ct)) ?? []
            : [];
}
