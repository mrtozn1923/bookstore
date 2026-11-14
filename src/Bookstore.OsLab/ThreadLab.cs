using System.Diagnostics;

namespace Bookstore.OsLab;

public sealed record ThreadCost(int Waiters, int OsThreads, double Milliseconds, long VirtualDeltaMb, long ResidentDeltaMb);

public static class ThreadLab
{
    // Linux'ta /proc/thread-self, "<pid>/task/<tid>" hedefine giden bir symlink'tir
    public static string OsThreadId() =>
        OperatingSystem.IsLinux()
            ? Path.GetFileName(new FileInfo("/proc/thread-self").LinkTarget ?? "?")
            : "n/a";

    public static void Describe(string label) =>
        Console.WriteLine($"{label,-14} managed id={Environment.CurrentManagedThreadId,-3} os tid={OsThreadId()}");

    public static ThreadCost Measure(int count, int maxStackSize = 0)
    {
        using ManualResetEventSlim release = new();
        using Process self = Process.GetCurrentProcess();
        long virtualBefore = self.VirtualMemorySize64;
        long residentBefore = self.WorkingSet64;

        Stopwatch sw = Stopwatch.StartNew();
        List<Thread> threads = new(count);
        for (int i = 0; i < count; i++)
        {
            // Her istek için bir thread açan eski usul sunucuyu taklit eder
            Thread t = new(() => release.Wait(), maxStackSize) { IsBackground = true, Name = $"order-{i}" };
            t.Start();
            threads.Add(t);
        }
        double elapsed = sw.Elapsed.TotalMilliseconds;

        self.Refresh();
        ThreadCost cost = new(count, self.Threads.Count, elapsed,
            (self.VirtualMemorySize64 - virtualBefore) / 1024 / 1024,
            (self.WorkingSet64 - residentBefore) / 1024 / 1024);

        release.Set();
        foreach (Thread t in threads)
        {
            t.Join();
        }
        return cost;
    }

    public static async Task<ThreadCost> MeasureTasksAsync(int count)
    {
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using Process self = Process.GetCurrentProcess();
        long virtualBefore = self.VirtualMemorySize64;
        long residentBefore = self.WorkingSet64;

        Stopwatch sw = Stopwatch.StartNew();
        Task[] waiters = new Task[count];
        for (int i = 0; i < count; i++)
        {
            // Bekleyen iş bir thread değil, heap'teki küçük bir state machine
            waiters[i] = WaitAsync(release.Task);
        }
        double elapsed = sw.Elapsed.TotalMilliseconds;

        self.Refresh();
        ThreadCost cost = new(count, self.Threads.Count, elapsed,
            (self.VirtualMemorySize64 - virtualBefore) / 1024 / 1024,
            (self.WorkingSet64 - residentBefore) / 1024 / 1024);

        release.SetResult();
        await Task.WhenAll(waiters);
        return cost;
    }

    private static async Task WaitAsync(Task signal) => await signal;
}

public static class ContextDemo
{
    [ThreadStatic] private static string? s_threadStaticUser;
    private static readonly AsyncLocal<string?> s_asyncLocalUser = new();

    public static async Task<string> RunAsync()
    {
        s_threadStaticUser = "Ada";
        s_asyncLocalUser.Value = "Ada";
        int before = Environment.CurrentManagedThreadId;

        await Task.Delay(50);

        int after = Environment.CurrentManagedThreadId;
        return $"thread {before} -> {after}: ThreadStatic={s_threadStaticUser ?? "null"}, AsyncLocal={s_asyncLocalUser.Value ?? "null"}";
    }
}
