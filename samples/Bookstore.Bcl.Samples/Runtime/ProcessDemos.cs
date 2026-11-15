using System.Diagnostics;
using Bookstore.Bcl.Processes;

namespace Bookstore.Bcl.Samples.Runtime;

public static class ProcessDemos
{
    public static IEnumerable<(string Name, Func<Task> Run)> All =>
    [
        ("proc-tar", TarAsync),
        ("proc-deadlock", DeadlockAsync),
        ("proc-noisy", NoisyAsync),
        ("proc-args", ArgumentsAsync),
        ("proc-shell", ShellAsync),
    ];

    private static string WorkDirectory()
    {
        string dir = Path.Combine(Path.GetTempPath(), "bookstore-nightly");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static async Task TarAsync()
    {
        string dir = WorkDirectory();
        await File.WriteAllTextAsync(Path.Combine(dir, "orders.csv"), "OrderNumber,Total\nBK-1001,349.90\n");

        ProcessStartInfo startInfo = new("tar")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = dir,
        };
        startInfo.ArgumentList.Add("-czf");
        startInfo.ArgumentList.Add("nightly-2026-11-14.tar.gz");
        startInfo.ArgumentList.Add("orders.csv");

        using Process process = Process.Start(startInfo)!;
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        Console.WriteLine($"tar -czf: exit={process.ExitCode} stdout=\"{await stdout}\" stderr=\"{await stderr}\"");

        ToolResult list = await ExternalTool.RunAsync("tar", ["-tzf", "nightly-2026-11-14.tar.gz"], TimeSpan.FromSeconds(10), dir);
        Console.WriteLine($"tar -tzf: exit={list.ExitCode} files={list.StandardOutput.Trim()}");

        ToolResult missing = await ExternalTool.RunAsync("tar", ["-czf", "broken.tar.gz", "missing.csv"], TimeSpan.FromSeconds(10), dir);
        Console.WriteLine($"missing : exit={missing.ExitCode}");
        Console.Write($"stderr  : {missing.StandardError}");
    }

    private static ProcessStartInfo SelfStartInfo(string demo)
    {
        // "dotnet run" starts the app through its apphost, "dotnet app.dll" through dotnet.
        string host = Environment.ProcessPath!;
        ProcessStartInfo startInfo = new(host) { RedirectStandardOutput = true, RedirectStandardError = true };
        if (Path.GetFileNameWithoutExtension(host) == "dotnet")
            startInfo.ArgumentList.Add(typeof(ProcessDemos).Assembly.Location);
        startInfo.ArgumentList.Add(demo);
        return startInfo;
    }

    private static Task NoisyAsync()
    {
        // 1 MB of warnings to stderr, a one-line result to stdout.
        Console.Error.Write(new string('w', 1024 * 1024));
        Console.WriteLine("report done");
        return Task.CompletedTask;
    }

    private static async Task DeadlockAsync()
    {
        using (Process child = Process.Start(SelfStartInfo("proc-noisy"))!)
        {
            try
            {
                // Wrong: read stdout to the end first, look at stderr later.
                string output = await child.StandardOutput.ReadToEndAsync().WaitAsync(TimeSpan.FromSeconds(5));
                Console.WriteLine($"stdout: {output}");
            }
            catch (TimeoutException)
            {
                Console.WriteLine($"stdout-first: no result after 5 s, child exited: {child.HasExited}");
                child.Kill(entireProcessTree: true);
                await child.WaitForExitAsync();
            }
        }

        ProcessStartInfo self = SelfStartInfo("proc-noisy");
        ToolResult result = await ExternalTool.RunAsync(self.FileName, self.ArgumentList, TimeSpan.FromSeconds(30));
        Console.WriteLine($"both pipes  : exit={result.ExitCode} stdout=\"{result.StandardOutput.Trim()}\" stderr={result.StandardError.Length:N0} chars");
    }

    private static async Task ArgumentsAsync()
    {
        string dir = WorkDirectory();
        string fileName = "Q4 report.csv";
        await File.WriteAllTextAsync(Path.Combine(dir, fileName), "OrderNumber,Total\n");

        // Wrong: joining the arguments into one string.
        ProcessStartInfo joined = new("tar", $"-czf q4.tar.gz {fileName}") { WorkingDirectory = dir, RedirectStandardError = true };
        using (Process process = Process.Start(joined)!)
        {
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            Console.WriteLine($"Arguments string: exit={process.ExitCode}");
            Console.Write(error);
        }

        ToolResult listed = await ExternalTool.RunAsync("tar", ["-czf", "q4.tar.gz", fileName], TimeSpan.FromSeconds(10), dir);
        Console.WriteLine($"ArgumentList    : exit={listed.ExitCode}");
    }

    private static async Task ShellAsync()
    {
        string title = "Dune; echo INJECTED";

        // Wrong: embedding user data in a shell command.
        ToolResult viaShell = await ExternalTool.RunAsync("sh", ["-c", $"echo Archiving {title}"], TimeSpan.FromSeconds(10));
        Console.Write($"sh -c (concatenated):\n{viaShell.StandardOutput}");

        ToolResult asArgument = await ExternalTool.RunAsync("sh", ["-c", "echo Archiving \"$1\"", "sh", title], TimeSpan.FromSeconds(10));
        Console.Write($"sh -c (positional \"$1\"):\n{asArgument.StandardOutput}");
    }
}
