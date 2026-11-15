using System.Runtime.InteropServices;

namespace Bookstore.Bcl.Samples.Cli;

public static class ManualArgsDemos
{
    public static IEnumerable<(string Name, Action Run)> All =>
    [
        ("cli-manual", Manual),
    ];

    public static IEnumerable<(string Name, Func<Task> Run)> AllAsync =>
    [
        ("cli-signal", SignalAsync),
    ];

    private static void Manual()
    {
        string[][] inputs =
        [
            ["import-prices", "prices.csv", "--dry-run"],
            ["import-prices", "prices.csv", "--dryrun"],
            ["import-prices", "--dry-run", "prices.csv"],
            ["import-prices"],
        ];

        foreach (string[] args in inputs)
        {
            string line = string.Join(' ', args);
            try
            {
                (string file, bool dryRun) = ParseManually(args);
                Console.WriteLine($"{line,-38} -> file={file}, dryRun={dryRun}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{line,-38} -> {ex.GetType().Name}");
            }
        }
    }

    // The first version everybody writes: command, file, then flags.
    private static (string File, bool DryRun) ParseManually(string[] args)
    {
        string file = args[1];
        bool dryRun = args.Contains("--dry-run");
        return (file, dryRun);
    }

    private static async Task SignalAsync()
    {
        using CancellationTokenSource cts = new();
        PosixSignal? received = null;

        void OnSignal(PosixSignalContext context)
        {
            context.Cancel = true; // Let the app exit on its own instead of being terminated right away.
            received = context.Signal;
            cts.Cancel();
        }

        using PosixSignalRegistration sigint = PosixSignalRegistration.Create(PosixSignal.SIGINT, OnSignal);
        using PosixSignalRegistration sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnSignal);

        int batches = 0;
        try
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), cts.Token);
                Console.WriteLine($"batch {++batches} done");
            }
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine($"received {received}, stopped cleanly after {batches} batches");
        }
    }
}
