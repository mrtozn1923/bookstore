namespace Algorithms.Intervals;

public static class IntervalOps
{
    // Sort by start, then extend the last merged interval while the next one overlaps (or touches)
    public static List<Interval<T>> Merge<T>(IEnumerable<Interval<T>> intervals, bool mergeTouching = true)
        where T : IComparable<T>
    {
        List<Interval<T>> sorted = [.. intervals.Where(i => !i.IsEmpty).OrderBy(i => i.Start)];
        List<Interval<T>> merged = [];
        foreach (Interval<T> current in sorted)
        {
            if (merged.Count > 0)
            {
                Interval<T> last = merged[^1];
                int cmp = current.Start.CompareTo(last.End);
                if (cmp < 0 || (cmp == 0 && mergeTouching))
                {
                    if (current.End.CompareTo(last.End) > 0) merged[^1] = last with { End = current.End };
                    continue;
                }
            }

            merged.Add(current);
        }

        return merged;
    }

    // After sorting, only neighbours can be the first overlapping pair
    public static (Interval<T> First, Interval<T> Second)? FindOverlap<T>(IEnumerable<Interval<T>> intervals)
        where T : IComparable<T>
    {
        Interval<T>[] sorted = [.. intervals.OrderBy(i => i.Start)];
        Interval<T> reach = default;
        for (int i = 0; i < sorted.Length; i++)
        {
            if (i > 0 && sorted[i].Start.CompareTo(reach.End) < 0) return (reach, sorted[i]);
            if (i == 0 || sorted[i].End.CompareTo(reach.End) > 0) reach = sorted[i];
        }

        return null;
    }

    // Sweep line: +1 at every start, -1 at every end; at equal times the end comes first (half-open)
    public static (int Max, T At) MaxConcurrent<T>(IEnumerable<Interval<T>> intervals) where T : IComparable<T>
    {
        List<(T Time, int Delta)> events = [];
        foreach (Interval<T> i in intervals)
        {
            events.Add((i.Start, +1));
            events.Add((i.End, -1));
        }

        events.Sort((a, b) =>
        {
            int c = a.Time.CompareTo(b.Time);
            return c != 0 ? c : a.Delta.CompareTo(b.Delta); // -1 before +1
        });

        int current = 0, max = 0;
        T at = default!;
        foreach ((T time, int delta) in events)
        {
            current += delta;
            if (current > max)
            {
                max = current;
                at = time;
            }
        }

        return (max, at);
    }

    // Gaps inside [window.Start, window.End) not covered by any busy interval
    public static List<Interval<T>> Gaps<T>(Interval<T> window, IEnumerable<Interval<T>> busy) where T : IComparable<T>
    {
        List<Interval<T>> gaps = [];
        T cursor = window.Start;
        foreach (Interval<T> b in Merge(busy))
        {
            if (b.End.CompareTo(window.Start) <= 0 || b.Start.CompareTo(window.End) >= 0) continue;
            if (b.Start.CompareTo(cursor) > 0) gaps.Add(new Interval<T>(cursor, b.Start));
            if (b.End.CompareTo(cursor) > 0) cursor = b.End;
        }

        if (cursor.CompareTo(window.End) < 0) gaps.Add(new Interval<T>(cursor, window.End));
        return gaps;
    }
}
