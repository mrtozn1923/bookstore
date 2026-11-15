namespace Algorithms.Randomness;

public static class Shuffles
{
    // Fisher-Yates (Durstenfeld): walk from the end, swap i with a random index in [0, i]
    public static void FisherYates<T>(Span<T> items, Random random)
    {
        for (int i = items.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }

    // Looks almost the same, but j comes from the whole array: n^n paths for n! permutations, so it is biased
    public static void NaiveSwap<T>(Span<T> items, Random random)
    {
        for (int i = 0; i < items.Length; i++)
        {
            int j = random.Next(items.Length);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }

    // A comparer that answers at random breaks the sort's assumptions
    public static void SortWithRandomComparer<T>(T[] items, Random random) =>
        Array.Sort(items, (_, _) => random.Next(-1, 2));
}
