using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Bookstore.Web.Vite;

public sealed record ViteChunk(string File, string? Src, bool IsEntry, IReadOnlyList<string>? Css, IReadOnlyList<string>? Imports);

public sealed class ViteManifest(IWebHostEnvironment environment, IOptions<ViteOptions> options)
{
    private readonly Lazy<IReadOnlyDictionary<string, ViteChunk>> chunks = new(() =>
    {
        string path = Path.Combine(environment.WebRootPath, options.Value.OutputPath, ".vite", "manifest.json");
        using FileStream stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<Dictionary<string, ViteChunk>>(stream, JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException($"Empty Vite manifest: {path}");
    });

    public ViteChunk GetEntry(string src) =>
        chunks.Value.TryGetValue(src, out ViteChunk? chunk) && chunk.IsEntry
            ? chunk
            : throw new InvalidOperationException($"'{src}' is not an entry in the Vite manifest. Did you run 'npm run build:server'?");

    // Chunks the entry imports statically, dependencies first (same walk as the Vite docs)
    public IReadOnlyList<ViteChunk> GetImportedChunks(ViteChunk entry)
    {
        List<ViteChunk> result = [];
        HashSet<string> seen = [];
        Visit(entry);
        return result;

        void Visit(ViteChunk chunk)
        {
            foreach (string key in chunk.Imports ?? [])
            {
                if (seen.Add(key))
                {
                    ViteChunk imported = chunks.Value[key];
                    Visit(imported);
                    result.Add(imported);
                }
            }
        }
    }
}
