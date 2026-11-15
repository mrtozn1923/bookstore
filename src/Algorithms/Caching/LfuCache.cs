namespace Algorithms.Caching;

// O(1) LFU: one LinkedList per frequency; ties inside a frequency are broken by recency
public sealed class LfuCache<TKey, TValue> where TKey : notnull
{
    private sealed class Entry(TKey key, TValue value)
    {
        public TKey Key { get; } = key;
        public TValue Value { get; set; } = value;
        public int Frequency { get; set; } = 1;
    }

    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<Entry>> _map = [];
    private readonly Dictionary<int, LinkedList<Entry>> _byFrequency = [];
    private int _minFrequency;

    public LfuCache(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _capacity = capacity;
    }

    public int Count => _map.Count;

    public bool TryGet(TKey key, out TValue value)
    {
        if (!_map.TryGetValue(key, out LinkedListNode<Entry>? node))
        {
            value = default!;
            return false;
        }

        Touch(node);
        value = node.Value.Value;
        return true;
    }

    public void Put(TKey key, TValue value)
    {
        if (_map.TryGetValue(key, out LinkedListNode<Entry>? node))
        {
            node.Value.Value = value;
            Touch(node);
            return;
        }

        if (_map.Count == _capacity)
        {
            LinkedList<Entry> coldest = _byFrequency[_minFrequency];
            Entry victim = coldest.Last!.Value; // least recent among the least frequent
            coldest.RemoveLast();
            if (coldest.Count == 0) _byFrequency.Remove(_minFrequency);
            _map.Remove(victim.Key);
        }

        _map[key] = Bucket(1).AddFirst(new Entry(key, value));
        _minFrequency = 1;
    }

    public int FrequencyOf(TKey key) => _map.TryGetValue(key, out LinkedListNode<Entry>? n) ? n.Value.Frequency : 0;

    private void Touch(LinkedListNode<Entry> node)
    {
        int f = node.Value.Frequency;
        LinkedList<Entry> list = _byFrequency[f];
        list.Remove(node);
        if (list.Count == 0)
        {
            _byFrequency.Remove(f);
            if (_minFrequency == f) _minFrequency = f + 1;
        }

        node.Value.Frequency = f + 1;
        Bucket(f + 1).AddFirst(node);
    }

    private LinkedList<Entry> Bucket(int frequency)
    {
        if (!_byFrequency.TryGetValue(frequency, out LinkedList<Entry>? list))
        {
            list = new LinkedList<Entry>();
            _byFrequency[frequency] = list;
        }

        return list;
    }
}
