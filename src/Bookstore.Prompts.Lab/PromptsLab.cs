using System.Net.Http.Json;
using System.Text.Json;
using Bookstore.Prompts.Catalog;

namespace Bookstore.Prompts.Lab;

public static class PromptsLab
{
    public static void List()
    {
        foreach (PromptTemplate t in PromptCatalog.Default.All.OrderBy(t => t.Key))
        {
            Console.WriteLine($"{t.Key,-17} {t.Fingerprint}  {t.Metadata.Model}@{t.Metadata.ModelDigest}  temperature={t.Metadata.Temperature} seed={t.Metadata.Seed}");
        }
    }

    // Writes the lock file; in a pull request its diff shows exactly which prompt versions are new
    public static void Lock(string path)
    {
        SortedDictionary<string, string> entries = new(PromptCatalog.Default.All.ToDictionary(t => t.Key, t => t.Fingerprint), StringComparer.Ordinal);
        File.WriteAllText(path, JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        Console.WriteLine($"locked {entries.Count} prompts in {path}");
    }

    sealed record Tags(List<TagModel> Models);

    sealed record TagModel(string Name, string Digest);

    // A model tag such as gemma3:4b can be re-pointed; the digest pins the exact weights the prompt was measured with
    public static async Task CheckModelsAsync()
    {
        using HttpClient http = new() { BaseAddress = new Uri(LabClient.Endpoint) };
        Tags tags = (await http.GetFromJsonAsync<Tags>("/api/tags", new JsonSerializerOptions(JsonSerializerDefaults.Web)))!;
        foreach (PromptTemplate t in PromptCatalog.Default.All.OrderBy(t => t.Key))
        {
            string? digest = tags.Models.FirstOrDefault(m => m.Name == t.Metadata.Model)?.Digest;
            string status = digest is null ? "MISSING"
                : digest.StartsWith(t.Metadata.ModelDigest, StringComparison.Ordinal) ? "OK" : $"MISMATCH (server has {digest[..12]})";
            Console.WriteLine($"{t.Key,-17} {t.Metadata.Model}@{t.Metadata.ModelDigest} {status}");
        }
    }
}
