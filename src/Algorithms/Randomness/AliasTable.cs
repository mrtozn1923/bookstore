namespace Algorithms.Randomness;

// Walker's alias method with Vose's construction: O(n) to build, O(1) per pick
public sealed class AliasTable<T>
{
    private readonly T[] _items;
    private readonly double[] _probability;
    private readonly int[] _alias;

    public AliasTable(IReadOnlyList<(T Item, double Weight)> weighted)
    {
        int n = weighted.Count;
        double total = weighted.Sum(w => w.Weight);
        _items = [.. weighted.Select(w => w.Item)];
        _probability = new double[n];
        _alias = new int[n];
        double[] scaled = [.. weighted.Select(w => w.Weight * n / total)]; // average becomes 1
        Stack<int> small = new(), large = new();
        for (int i = 0; i < n; i++) (scaled[i] < 1 ? small : large).Push(i);
        while (small.Count > 0 && large.Count > 0)
        {
            int s = small.Pop(), l = large.Pop();
            _probability[s] = scaled[s];
            _alias[s] = l;                        // the rest of column s belongs to l
            scaled[l] -= 1 - scaled[s];
            (scaled[l] < 1 ? small : large).Push(l);
        }

        foreach (int i in large) _probability[i] = 1;
        foreach (int i in small) _probability[i] = 1; // only rounding leftovers
    }

    public T Pick(Random random)
    {
        int column = random.Next(_items.Length);
        return random.NextDouble() < _probability[column] ? _items[column] : _items[_alias[column]];
    }
}
