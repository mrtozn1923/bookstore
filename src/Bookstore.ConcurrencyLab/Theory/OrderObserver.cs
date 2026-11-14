using System.Collections.Concurrent;

namespace Bookstore.ConcurrencyLab.Theory;

public static class OrderObserver
{
    // Runs two threads of three steps many times and counts the observed global orders
    public static Dictionary<string, int> Observe(int runs)
    {
        Dictionary<string, int> histogram = [];
        for (int run = 0; run < runs; run++)
        {
            ConcurrentQueue<string> log = new();
            using Barrier start = new(2);
            Thread a = new(() => Steps("A", log, start));
            Thread b = new(() => Steps("B", log, start));
            a.Start(); b.Start();
            a.Join(); b.Join();
            string key = string.Concat(log.Select(s => s[0]));
            histogram[key] = histogram.GetValueOrDefault(key) + 1;
        }
        return histogram;
    }

    private static void Steps(string name, ConcurrentQueue<string> log, Barrier start)
    {
        start.SignalAndWait();
        for (int i = 1; i <= 3; i++)
            log.Enqueue($"{name}{i}");
    }
}
