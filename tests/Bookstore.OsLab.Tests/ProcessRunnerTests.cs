using System.Diagnostics;
using Xunit;

namespace Bookstore.OsLab.Tests;

public sealed class ProcessRunnerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task Large_output_on_both_pipes_does_not_deadlock()
    {
        ProcessResult r = await ProcessRunner.RunAsync("dotnet", ReportTool.Args("sales", "20000"), Timeout);

        Assert.False(r.TimedOut);
        Assert.Equal(0, r.ExitCode);
        Assert.Equal(20_001, r.StdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.Contains("row 20000 of 20000", r.StdErr);
    }

    [Fact]
    public async Task Exit_code_and_stderr_are_returned()
    {
        ProcessResult r = await ProcessRunner.RunAsync("dotnet", ReportTool.Args("sales", "5", "--fail"), Timeout);

        Assert.Equal(2, r.ExitCode);
        Assert.Contains("warehouse data is incomplete", r.StdErr);
    }

    [Fact]
    public async Task Timeout_kills_the_child()
    {
        Stopwatch sw = Stopwatch.StartNew();
        ProcessResult r = await ProcessRunner.RunAsync("dotnet", ReportTool.Args("sleep", "30"), TimeSpan.FromSeconds(1));

        Assert.True(r.TimedOut);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(15));
    }

    [Fact]
    public async Task Timeout_also_kills_grandchildren_that_hold_the_pipe()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "uses /bin/sh");
        Stopwatch sw = Stopwatch.StartNew();

        // Arka plandaki sleep, stdout pipe'ını miras alıp açık tutar
        ProcessResult r = await ProcessRunner.RunAsync("/bin/sh", ["-c", "sleep 30 & sleep 30"], TimeSpan.FromSeconds(1));

        Assert.True(r.TimedOut);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(15));
    }

    [Fact]
    public async Task ArgumentList_passes_shell_metacharacters_verbatim()
    {
        ProcessResult r = await ProcessRunner.RunAsync("dotnet", ReportTool.Args("args", "Ada; echo INJECTED"), Timeout);

        Assert.Equal("[Ada; echo INJECTED]", r.StdOut.Trim());
    }

    [Fact]
    public async Task Hidden_variables_are_not_inherited()
    {
        Environment.SetEnvironmentVariable("BOOKSTORE_TEST_SECRET", "s3cr3t");
        string[] toolArgs = ReportTool.Args("env", "BOOKSTORE_TEST_SECRET");

        ProcessResult inherited = await ProcessRunner.RunAsync("dotnet", toolArgs, Timeout);
        ProcessResult hidden = await ProcessRunner.RunAsync("dotnet", toolArgs, Timeout, hiddenVariables: ["BOOKSTORE_TEST_SECRET"]);

        Assert.Equal("BOOKSTORE_TEST_SECRET=s3cr3t", inherited.StdOut.Trim());
        Assert.Equal("BOOKSTORE_TEST_SECRET is not set", hidden.StdOut.Trim());
    }
}
