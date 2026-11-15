using System.Collections.Concurrent;

namespace Bookstore.Api.Catalog.Seo;

// Which batches were sent and not yet collected. Persist it: after a restart the worker polls instead of sending again.
public interface ISeoBatchJobStore
{
    IReadOnlyList<string> Pending();
    void Add(string batchId);
    void Complete(string batchId);
}

public interface IBookSeoStore
{
    bool HasDescription(string isbn);
    void Save(string isbn, string description);
}

public sealed class InMemorySeoBatchJobStore : ISeoBatchJobStore
{
    private readonly ConcurrentDictionary<string, byte> _pending = new();

    public IReadOnlyList<string> Pending() => _pending.Keys.Order(StringComparer.Ordinal).ToList();
    public void Add(string batchId) => _pending[batchId] = 0;
    public void Complete(string batchId) => _pending.TryRemove(batchId, out _);
}

public sealed class InMemoryBookSeoStore : IBookSeoStore
{
    private readonly ConcurrentDictionary<string, string> _descriptions = new();

    public bool HasDescription(string isbn) => _descriptions.ContainsKey(isbn);
    public void Save(string isbn, string description) => _descriptions[isbn] = description;
    public string? Find(string isbn) => _descriptions.TryGetValue(isbn, out string? text) ? text : null;
}
