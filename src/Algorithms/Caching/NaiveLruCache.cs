namespace Algorithms.Caching;

// Recency kept in a List<TKey>: every hit is a linear Remove, so get/put are O(n)
public sealed class NaiveLruCache<TKey, TValue>(int capacity) where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _map = new(capacity);
    private readonly List<TKey> _order = new(capacity); // index 0 = least recent

    public bool TryGet(TKey key, out TValue value)
    {
        if (!_map.TryGetValue(key, out value!)) return false;
        _order.Remove(key);
        _order.Add(key);
        return true;
    }

    public void Put(TKey key, TValue value)
    {
        if (_map.ContainsKey(key))
        {
            _order.Remove(key);
        }
        else if (_map.Count == capacity)
        {
            _map.Remove(_order[0]);
            _order.RemoveAt(0);
        }

        _map[key] = value;
        _order.Add(key);
    }
}
