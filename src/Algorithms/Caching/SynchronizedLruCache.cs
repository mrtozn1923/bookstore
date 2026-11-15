namespace Algorithms.Caching;

// Even TryGet changes the list order, so reads need the same lock as writes
public sealed class SynchronizedLruCache<TKey, TValue>(int capacity) where TKey : notnull
{
    private readonly LruCache<TKey, TValue> _inner = new(capacity);
    private readonly Lock _gate = new();

    public int Count { get { lock (_gate) return _inner.Count; } }

    public bool TryGet(TKey key, out TValue value)
    {
        lock (_gate) return _inner.TryGet(key, out value);
    }

    public void Put(TKey key, TValue value)
    {
        lock (_gate) _inner.Put(key, value);
    }
}
