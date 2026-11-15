using System.Collections.Concurrent;
using System.Text.Json;

namespace Bookstore.FileDrops;

public sealed record LedgerEntry(string Scope, string Key, string Detail, DateTimeOffset At);

// What has already been processed, kept outside the drop folder: file hashes, bank transaction ids.
// A JSON-lines file in this sample; in Bookstore a table with a unique (Scope, Key) index.
public sealed class FileDropLedger
{
    private readonly ConcurrentDictionary<(string, string), LedgerEntry> _entries = new();
    private readonly string _path;
    private readonly Lock _gate = new();

    public FileDropLedger(string path)
    {
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        if (File.Exists(path))
        {
            foreach (var line in File.ReadLines(path))
            {
                var entry = JsonSerializer.Deserialize<LedgerEntry>(line)!;
                _entries[(entry.Scope, entry.Key)] = entry;
            }
        }
    }

    public LedgerEntry? Find(string scope, string key) => _entries.GetValueOrDefault((scope, key));

    public void Add(string scope, string key, string detail, DateTimeOffset at)
    {
        var entry = new LedgerEntry(scope, key, detail, at);
        lock (_gate)
        {
            if (_entries.TryAdd((scope, key), entry))
            {
                File.AppendAllLines(_path, [JsonSerializer.Serialize(entry)]);
            }
        }
    }
}
