namespace Algorithms.Caching;

// Dictionary: key -> list node (O(1) lookup); LinkedList: most recent first, least recent last
public sealed class LruCache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> _map;
    private readonly LinkedList<KeyValuePair<TKey, TValue>> _order = new();

    public LruCache(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _capacity = capacity;
        _map = new Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>(capacity);
    }

    public event Action<TKey, TValue>? Evicted;

    public int Count => _map.Count;

    public IEnumerable<TKey> KeysMostRecentFirst => _order.Select(p => p.Key);

    public bool TryGet(TKey key, out TValue value)
    {
        if (_map.TryGetValue(key, out LinkedListNode<KeyValuePair<TKey, TValue>>? node))
        {
            MoveToFront(node);
            value = node.Value.Value;
            return true;
        }

        value = default!;
        return false;
    }

    public void Put(TKey key, TValue value)
    {
        if (_map.TryGetValue(key, out LinkedListNode<KeyValuePair<TKey, TValue>>? node))
        {
            node.Value = new(key, value);
            MoveToFront(node);
            return;
        }

        if (_map.Count == _capacity)
        {
            LinkedListNode<KeyValuePair<TKey, TValue>> lru = _order.Last!;
            _order.RemoveLast();
            _map.Remove(lru.Value.Key);
            Evicted?.Invoke(lru.Value.Key, lru.Value.Value);
        }

        _map[key] = _order.AddFirst(new KeyValuePair<TKey, TValue>(key, value));
    }

    public bool Remove(TKey key)
    {
        if (!_map.Remove(key, out LinkedListNode<KeyValuePair<TKey, TValue>>? node))
            return false;
        _order.Remove(node); // the node must leave the list too
        return true;
    }

    internal int ListCount => _order.Count;

    private void MoveToFront(LinkedListNode<KeyValuePair<TKey, TValue>> node)
    {
        if (node == _order.First) return;
        _order.Remove(node); // O(1): we already hold the node
        _order.AddFirst(node);
    }
}
