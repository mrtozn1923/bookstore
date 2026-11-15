using System.Diagnostics;

namespace Bookstore.Bcl.Processes;

public sealed record ToolResult(int ExitCode, string StandardOutput, string StandardError, TimeSpan Elapsed, bool TimedOut);

// Runs an external command without a shell, captures both pipes and enforces a timeout.
public static class ExternalTool
{
    public static async Task<ToolResult> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        TimeSpan timeout,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        ProcessStartInfo startInfo = new(fileName)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = workingDirectory ?? "",
        };
        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);

        long started = Stopwatch.GetTimestamp();
        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start '{fileName}'.");

        // Read both pipes at the same time; reading one of them to the end first can deadlock.
        Task<string> stdout = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        Task<string> stderr = process.StandardError.ReadToEndAsync(CancellationToken.None);

        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);
        bool timedOut = false;
        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
            cancellationToken.ThrowIfCancellationRequested();
            timedOut = true;
        }

        string[] output = await Task.WhenAll(stdout, stderr);
        return new ToolResult(process.ExitCode, output[0], output[1], Stopwatch.GetElapsedTime(started), timedOut);
    }
}
