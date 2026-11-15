namespace Algorithms.Randomness;

// Cumulative weights + binary search: O(n) to build, O(log n) per pick
public sealed class WeightedPicker<T>
{
    private readonly T[] _items;
    private readonly double[] _cumulative;

    public WeightedPicker(IEnumerable<(T Item, double Weight)> weighted)
    {
        (T Item, double Weight)[] all = [.. weighted.Where(w => w.Weight > 0)];
        if (all.Length == 0) throw new ArgumentException("At least one positive weight is required.");
        _items = [.. all.Select(w => w.Item)];
        _cumulative = new double[all.Length];
        double total = 0;
        for (int i = 0; i < all.Length; i++) _cumulative[i] = total += all[i].Weight;
    }

    public T Pick(Random random)
    {
        double r = random.NextDouble() * _cumulative[^1];
        int index = Array.BinarySearch(_cumulative, r);
        index = index < 0 ? ~index : index + 1; // first cumulative value greater than r
        return _items[Math.Min(index, _items.Length - 1)];
    }
}
