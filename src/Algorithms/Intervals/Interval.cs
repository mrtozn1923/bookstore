namespace Algorithms.Intervals;

// Half-open [Start, End): Start belongs to the interval, End does not
public readonly record struct Interval<T>(T Start, T End) where T : IComparable<T>
{
    public bool IsEmpty => Start.CompareTo(End) >= 0;

    public bool Overlaps(Interval<T> other) =>
        Start.CompareTo(other.End) < 0 && other.Start.CompareTo(End) < 0;

    public bool Contains(T point) => Start.CompareTo(point) <= 0 && point.CompareTo(End) < 0;

    public override string ToString() => $"[{Start}, {End})";
}
