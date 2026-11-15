using System.Diagnostics;
using System.Globalization;
using Bookstore.Bcl.Processes;
using Microsoft.Extensions.Logging;

namespace Bookstore.Bcl.Samples.Runtime;

public static class StopwatchDemos
{
    public static IEnumerable<(string Name, Func<Task> Run)> All =>
    [
        ("sw-measure", MeasureAsync),
        ("sw-alloc", AllocationsAsync),
        ("nightly-report", NightlyReportAsync),
    ];

    private static async Task MeasureAsync()
    {
        Console.WriteLine($"Stopwatch.IsHighResolution: {Stopwatch.IsHighResolution}, Frequency: {Stopwatch.Frequency:N0} ticks/s");

        Stopwatch stopwatch = Stopwatch.StartNew();
        await Task.Delay(200);
        stopwatch.Stop();
        Console.WriteLine($"Stopwatch        : {stopwatch.Elapsed.TotalMilliseconds:F1} ms");

        long started = Stopwatch.GetTimestamp();
        await Task.Delay(200);
        TimeSpan elapsed = Stopwatch.GetElapsedTime(started);
        Console.WriteLine($"GetElapsedTime   : {elapsed.TotalMilliseconds:F1} ms");

        stopwatch.Restart();
        await Task.Delay(50);
        TimeSpan lap = stopwatch.Elapsed;
        await Task.Delay(50);
        Console.WriteLine($"Lap / total      : {lap.TotalMilliseconds:F0} ms / {stopwatch.Elapsed.TotalMilliseconds:F0} ms (still running: {stopwatch.IsRunning})");
    }

    private static Task AllocationsAsync()
    {
        _ = Stopwatch.StartNew();
        _ = Stopwatch.GetElapsedTime(Stopwatch.GetTimestamp());

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1_000; i++)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            stopwatch.Stop();
        }
        long withStopwatch = GC.GetAllocatedBytesForCurrentThread() - before;

        before = GC.GetAllocatedBytesForCurrentThread();
        TimeSpan total = TimeSpan.Zero;
        for (int i = 0; i < 1_000; i++)
        {
            long started = Stopwatch.GetTimestamp();
            total += Stopwatch.GetElapsedTime(started);
        }
        long withTimestamp = GC.GetAllocatedBytesForCurrentThread() - before;

        Console.WriteLine($"1,000 x Stopwatch.StartNew()      : {withStopwatch:N0} bytes allocated");
        Console.WriteLine($"1,000 x GetTimestamp/GetElapsedTime: {withTimestamp:N0} bytes allocated");
        return Task.CompletedTask;
    }

    private static async Task NightlyReportAsync()
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            builder.AddSimpleConsole(options => options.SingleLine = true));
        ILogger logger = loggerFactory.CreateLogger("Bookstore.NightlyReport");

        string reportDir = Environment.GetEnvironmentVariable("BOOKSTORE_REPORT_DIR")
            ?? Path.Combine(Path.GetTempPath(), "bookstore-reports");
        Directory.CreateDirectory(reportDir);
        string day = "2026-11-14";

        long started = Stopwatch.GetTimestamp();
        string csvName = $"orders-{day}.csv";
        await File.WriteAllLinesAsync(Path.Combine(reportDir, csvName),
        [
            "OrderNumber,Total",
            .. Enumerable.Range(1001, 250).Select(n => string.Create(CultureInfo.InvariantCulture, $"BK-{n},{n % 7 * 50 + 99.90m}")),
        ]);
        logger.LogInformation("Wrote {File} in {ElapsedMs:F1} ms", csvName, Stopwatch.GetElapsedTime(started).TotalMilliseconds);

        string archive = $"nightly-{day}.tar.gz";
        ToolResult result = await ExternalTool.RunAsync("tar", ["-czf", archive, csvName], TimeSpan.FromSeconds(30), reportDir);
        if (result.TimedOut || result.ExitCode != 0)
        {
            logger.LogError("tar failed: exit={ExitCode} timedOut={TimedOut} stderr={StdErr}", result.ExitCode, result.TimedOut, result.StandardError.Trim());
            return;
        }

        long size = new FileInfo(Path.Combine(reportDir, archive)).Length;
        logger.LogInformation("Archived {Archive} ({Size} bytes), tar exit={ExitCode} in {ElapsedMs:F1} ms", archive, size, result.ExitCode, result.Elapsed.TotalMilliseconds);
        logger.LogInformation("Nightly report finished in {TotalMs:F1} ms", Stopwatch.GetElapsedTime(started).TotalMilliseconds);
    }
}
