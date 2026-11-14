using Xunit;

namespace Bookstore.OsLab.Tests;

public sealed class IoLabTests
{
    [Fact]
    public async Task Async_waits_do_not_need_a_thread_per_connection()
    {
        ConnectionReport async = await IoLab.IdleConnectionsAsync(200, blocking: false);
        ConnectionReport blocking = await IoLab.IdleConnectionsAsync(200, blocking: true);

        // Blocking modda en az bağlantı sayısı kadar ek thread olmalı
        Assert.True(blocking.OsThreads - async.OsThreads >= 150, $"async={async.OsThreads}, blocking={blocking.OsThreads}");
    }

    [Fact]
    public async Task Socket_engine_thread_watches_the_sockets_on_linux()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "reads /proc");

        await IoLab.IdleConnectionsAsync(5, blocking: false);

        Assert.Contains(IoLab.ThreadNames(), name => name.StartsWith(".NET Sockets"));
    }

    [Fact]
    public async Task Unix_domain_socket_round_trip_cleans_up_its_file()
    {
        string path = Path.Combine(Path.GetTempPath(), $"stock-{Guid.NewGuid():N}.sock");

        string[] lines = (await IoLab.UnixSocketRoundTripAsync(path)).ToArray();

        Assert.Equal("reply: stock 9780441172719 = 12", lines[1]);
        Assert.Equal("after Dispose, file exists: False", lines[2]);
    }

    [Fact]
    public async Task Named_pipe_delivers_the_message()
    {
        string name = $"bookstore-{Guid.NewGuid():N}";

        string[] lines = (await IoLab.NamedPipeRoundTripAsync(name)).ToArray();

        Assert.Equal("server received: report:daily", lines[0]);
    }
}
