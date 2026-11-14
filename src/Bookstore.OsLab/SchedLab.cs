using System.Diagnostics;

namespace Bookstore.OsLab;

public sealed record WorkerReport(string Name, long Iterations, long Voluntary, long Involuntary);

public static class SchedLab
{
    // /proc/thread-self/status: bu thread'in voluntary ve nonvoluntary context switch sayaçları
    public static (long Voluntary, long Involuntary) ContextSwitches()
    {
        if (!OperatingSystem.IsLinux())
        {
            return (-1, -1);
        }
        long voluntary = 0, involuntary = 0;
        foreach (string line in File.ReadLines("/proc/thread-self/status"))
        {
            if (line.StartsWith("voluntary_ctxt_switches:"))
                voluntary = long.Parse(line.Split(':')[1]);
            else if (line.StartsWith("nonvoluntary_ctxt_switches:"))
                involuntary = long.Parse(line.Split(':')[1]);
        }
        return (voluntary, involuntary);
    }

    // CPU'yu hiç bırakmayan, yalnız sayaç artıran bir iş
    public static long Spin(TimeSpan duration)
    {
        long iterations = 0;
        Stopwatch sw = Stopwatch.StartNew();
        while (sw.Elapsed < duration)
        {
            for (int i = 0; i < 10_000; i++)
            {
                iterations++;
            }
        }
        return iterations;
    }

    public static WorkerReport[] RunWorkers(int count, TimeSpan duration)
    {
        WorkerReport[] reports = new WorkerReport[count];
        Thread[] threads = new Thread[count];
        for (int i = 0; i < count; i++)
        {
            int index = i;
            threads[i] = new Thread(() =>
            {
                long iterations = Spin(duration);
                (long v, long n) = ContextSwitches();
                reports[index] = new WorkerReport($"worker-{index}", iterations, v, n);
            }) { Name = $"worker-{i}" };
        }
        foreach (Thread t in threads) t.Start();
        foreach (Thread t in threads) t.Join();
        return reports;
    }

    // İki thread sırayla birbirini uyandırır; AutoResetEvent spin yapmadan bloklar
    public static (TimeSpan Elapsed, long Voluntary) PingPong(int rounds)
    {
        using AutoResetEvent ping = new(false), pong = new(false);
        long ponger = 0;
        Thread other = new(() =>
        {
            for (int i = 0; i < rounds; i++)
            {
                ping.WaitOne();
                pong.Set();
            }
            ponger = ContextSwitches().Voluntary;
        });
        other.Start();

        long before = ContextSwitches().Voluntary;
        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < rounds; i++)
        {
            ping.Set();
            pong.WaitOne();
        }
        sw.Stop();
        other.Join();
        return (sw.Elapsed, ContextSwitches().Voluntary - before + ponger);
    }

    // Linux'ta nice değeri thread başınadır: /proc/self/task/<tid>/stat içindeki 19. alan
    public static IEnumerable<(string Tid, string Name, int Nice)> ThreadNiceValues()
    {
        foreach (string dir in Directory.GetDirectories("/proc/self/task"))
        {
            string stat = File.ReadAllText(Path.Combine(dir, "stat"));
            int close = stat.LastIndexOf(')');
            string name = stat[(stat.IndexOf('(') + 1)..close];
            string[] rest = stat[(close + 2)..].Split(' ');
            yield return (Path.GetFileName(dir), name, int.Parse(rest[16]));
        }
    }
}
