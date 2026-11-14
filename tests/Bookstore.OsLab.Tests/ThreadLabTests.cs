using Xunit;

namespace Bookstore.OsLab.Tests;

public sealed class ThreadLabTests
{
    [ThreadStatic] private static string? s_threadStaticUser;
    private static readonly AsyncLocal<string?> s_asyncLocalUser = new();

    [Fact]
    public void ThreadStatic_value_is_not_visible_on_another_thread()
    {
        s_threadStaticUser = "Ada";
        string? seen = "not run";

        Thread other = new(() => seen = s_threadStaticUser);
        other.Start();
        other.Join();

        Assert.Null(seen);
        Assert.Equal("Ada", s_threadStaticUser);
    }

    [Fact]
    public async Task AsyncLocal_value_flows_to_work_on_other_threads()
    {
        s_asyncLocalUser.Value = "Ada";

        string? seen = await Task.Run(() => s_asyncLocalUser.Value);

        Assert.Equal("Ada", seen);
    }

    [Fact]
    public async Task ContextDemo_loses_ThreadStatic_but_keeps_AsyncLocal_after_await()
    {
        string result = await Task.Run(ContextDemo.RunAsync);

        Assert.EndsWith("AsyncLocal=Ada", result);
    }

    [Fact]
    public void Measure_starts_one_os_thread_per_waiter()
    {
        ThreadCost cost = ThreadLab.Measure(50, maxStackSize: 256 * 1024);

        Assert.True(cost.OsThreads >= 50);
    }

    [Fact]
    public async Task Awaiting_tasks_do_not_need_their_own_threads()
    {
        ThreadCost cost = await ThreadLab.MeasureTasksAsync(10_000);

        Assert.True(cost.OsThreads < 100);
    }
}
