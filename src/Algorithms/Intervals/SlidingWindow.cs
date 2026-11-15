namespace Algorithms.Intervals;

public static class SlidingWindow
{
    // Monotonic deque of indexes: values increase from front to back, the front is the window minimum. O(n) in total
    public static decimal[] Minimums(IReadOnlyList<decimal> values, int window)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(window, 1);
        decimal[] result = new decimal[values.Count];
        int[] deque = new int[values.Count];
        int head = 0, tail = 0; // deque holds deque[head..tail)
        for (int i = 0; i < values.Count; i++)
        {
            while (tail > head && values[deque[tail - 1]] >= values[i]) tail--; // can never be a minimum again
            deque[tail++] = i;
            if (deque[head] <= i - window) head++; // fell out of the window on the left
            result[i] = values[deque[head]];
        }

        return result;
    }

    public static decimal[] MinimumsNaive(IReadOnlyList<decimal> values, int window)
    {
        decimal[] result = new decimal[values.Count];
        for (int i = 0; i < values.Count; i++)
        {
            decimal min = values[i];
            for (int j = Math.Max(0, i - window + 1); j < i; j++) min = Math.Min(min, values[j]);
            result[i] = min;
        }

        return result;
    }
}
