namespace Bookstore.ConcurrencyLab.Theory;

public static class Interleavings
{
    // Two step lists; every interleaving keeps each list's own order
    public static IEnumerable<string[]> Enumerate(string[] a, string[] b)
    {
        if (a.Length == 0) { yield return b; yield break; }
        if (b.Length == 0) { yield return a; yield break; }
        foreach (string[] rest in Enumerate(a[1..], b))
            yield return [a[0], .. rest];
        foreach (string[] rest in Enumerate(a, b[1..]))
            yield return [b[0], .. rest];
    }

    // (n*k)! / (k!)^n : n threads, k atomic steps each
    public static System.Numerics.BigInteger Count(int threads, int steps)
    {
        System.Numerics.BigInteger result = Factorial(threads * steps);
        for (int i = 0; i < threads; i++)
            result /= Factorial(steps);
        return result;
    }

    private static System.Numerics.BigInteger Factorial(int n)
    {
        System.Numerics.BigInteger f = 1;
        for (int i = 2; i <= n; i++) f *= i;
        return f;
    }

    // Simulates "stock = stock + 1" split into read / add / write for both threads
    public static int RunIncrement(string[] schedule)
    {
        int shared = 0;
        Dictionary<char, int> register = [];
        foreach (string step in schedule)
        {
            char thread = step[0];
            switch (step[1..])
            {
                case "read": register[thread] = shared; break;
                case "add": register[thread]++; break;
                case "write": shared = register[thread]; break;
            }
        }
        return shared;
    }
}
