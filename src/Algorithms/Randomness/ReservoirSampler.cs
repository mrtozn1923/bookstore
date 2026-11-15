namespace Algorithms.Randomness;

public static class ReservoirSampler
{
    // Algorithm R: keep the first k; the i-th item (0-based) replaces a random slot with probability k / (i + 1)
    public static T[] Sample<T>(IEnumerable<T> source, int k, Random random)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(k, 1);
        T[] reservoir = new T[k];
        long seen = 0;
        foreach (T item in source)
        {
            if (seen < k)
            {
                reservoir[seen] = item;
            }
            else
            {
                long j = random.NextInt64(seen + 1);
                if (j < k) reservoir[j] = item;
            }

            seen++;
        }

        return seen >= k ? reservoir : reservoir[..(int)seen];
    }
}
