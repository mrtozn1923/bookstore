using Xunit;

namespace Bookstore.OsLab.Tests;

public sealed class SchedLabTests
{
    [Fact]
    public void Every_worker_reports_its_own_counters()
    {
        WorkerReport[] reports = SchedLab.RunWorkers(3, TimeSpan.FromMilliseconds(200));

        Assert.Equal(3, reports.Length);
        Assert.All(reports, r => Assert.True(r.Iterations > 0));
    }

    [Fact]
    public void PingPong_blocks_in_the_kernel_on_every_handoff()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "reads /proc");

        (TimeSpan _, long voluntary) = SchedLab.PingPong(1_000);

        // Her turda iki thread de en az bir kez bloklanıp uyandırılır
        Assert.True(voluntary >= 1_000, $"voluntary={voluntary}");
    }

    [Fact]
    public void Nice_values_are_listed_per_thread()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "reads /proc");

        List<(string Tid, string Name, int Nice)> threads = SchedLab.ThreadNiceValues().ToList();

        Assert.Contains(threads, t => t.Tid == Environment.ProcessId.ToString());
        Assert.True(threads.Count > 1);
    }
}
