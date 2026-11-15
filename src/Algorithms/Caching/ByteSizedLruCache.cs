namespace Algorithms.Caching;

// Capacity measured in bytes, not entries: evict from the tail until the new item fits
public sealed class ByteSizedLruCache<TKey> where TKey : notnull
{
    private readonly long _maxBytes;
    private readonly Dictionary<TKey, LinkedListNode<(TKey Key, byte[] Data)>> _map = [];
    private readonly LinkedList<(TKey Key, byte[] Data)> _order = new();

    public ByteSizedLruCache(long maxBytes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxBytes, 1);
        _maxBytes = maxBytes;
    }

    public long CurrentBytes { get; private set; }

    public int Count => _map.Count;

    public bool TryGet(TKey key, out byte[] data)
    {
        if (_map.TryGetValue(key, out LinkedListNode<(TKey Key, byte[] Data)>? node))
        {
            _order.Remove(node);
            _order.AddFirst(node);
            data = node.Value.Data;
            return true;
        }

        data = [];
        return false;
    }

    public bool Put(TKey key, byte[] data)
    {
        if (data.Length > _maxBytes) return false; // never fits; do not flush the whole cache for it
        Remove(key);
        while (CurrentBytes + data.Length > _maxBytes)
        {
            (TKey oldKey, byte[] oldData) = _order.Last!.Value;
            _order.RemoveLast();
            _map.Remove(oldKey);
            CurrentBytes -= oldData.Length;
        }

        _map[key] = _order.AddFirst((key, data));
        CurrentBytes += data.Length;
        return true;
    }

    public bool Remove(TKey key)
    {
        if (!_map.Remove(key, out LinkedListNode<(TKey Key, byte[] Data)>? node)) return false;
        _order.Remove(node);
        CurrentBytes -= node.Value.Data.Length;
        return true;
    }
}
