using System.Diagnostics;

namespace Bookstore.OsLab;

public sealed record ProcessResult(int ExitCode, string StdOut, string StdErr, bool TimedOut);

public static class ProcessRunner
{
    public static async Task<ProcessResult> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        TimeSpan timeout,
        IEnumerable<string>? hiddenVariables = null)
    {
        ProcessStartInfo psi = new(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (string argument in arguments)
        {
            psi.ArgumentList.Add(argument); // shell yok, her argüman olduğu gibi gider
        }
        foreach (string name in hiddenVariables ?? [])
        {
            psi.Environment.Remove(name); // child'a miras kalmasın
        }

        using Process process = Process.Start(psi)!;

        // İki pipe aynı anda boşaltılır; hiçbiri dolup child'ı bloklayamaz
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();

        bool timedOut = false;
        using (CancellationTokenSource cts = new(timeout))
        {
            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                timedOut = true;
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }

        return new ProcessResult(process.ExitCode, await stdout, await stderr, timedOut);
    }
}
