namespace Bookstore.ConcurrencyLab.Deadlocks;

public static class Philosophers
{
    // Each philosopher takes the left fork, then the right one; returns false if the table froze
    public static bool Dine(int seats, int meals, bool orderedForks, TimeSpan patience)
    {
        object[] forks = [.. Enumerable.Range(0, seats).Select(_ => new object())];
        Thread[] diners = new Thread[seats];
        for (int p = 0; p < seats; p++)
        {
            int left = p, right = (p + 1) % seats;
            (int first, int second) = orderedForks && left > right ? (right, left) : (left, right);
            diners[p] = new Thread(() =>
            {
                for (int m = 0; m < meals; m++)
                {
                    lock (forks[first])
                    {
                        Thread.Yield();
                        lock (forks[second]) { }
                    }
                }
            }) { IsBackground = true };
        }
        foreach (Thread diner in diners) diner.Start();
        DateTime deadline = DateTime.UtcNow + patience;
        return diners.All(d => d.Join(Max(deadline - DateTime.UtcNow, TimeSpan.Zero)));
    }

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;
}
