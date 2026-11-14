using System.Diagnostics;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace Bookstore.OsLab;

public sealed record ConnectionReport(int Connections, string Mode, int OsThreads, double Milliseconds);

public static class IoLab
{
    public static int OsThreadCount() =>
        OperatingSystem.IsLinux() ? Directory.GetDirectories("/proc/self/task").Length : Process.GetCurrentProcess().Threads.Count;

    // Process'teki thread'leri adlarına göre gruplar: ".NET TP Worker x3" gibi
    public static IEnumerable<string> ThreadNames() =>
        Directory.GetDirectories("/proc/self/task")
            .Select(t => File.ReadAllText(Path.Combine(t, "comm")).Trim())
            .GroupBy(name => name)
            .OrderBy(g => g.Key)
            .Select(g => g.Count() == 1 ? g.Key : $"{g.Key} x{g.Count()}");

    // count bağlantı açar; sunucu tarafı her bağlantıda bir byte bekler
    public static async Task<ConnectionReport> IdleConnectionsAsync(int count, bool blocking)
    {
        Stopwatch sw = Stopwatch.StartNew();
        using Socket listener = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(count);

        List<Socket> clients = [];
        List<Socket> servers = [];
        List<Task> waits = [];
        List<Thread> threads = [];
        for (int i = 0; i < count; i++)
        {
            Socket client = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await client.ConnectAsync(listener.LocalEndPoint!);
            Socket server = await listener.AcceptAsync();
            clients.Add(client);
            servers.Add(server);

            byte[] buffer = new byte[1];
            if (blocking)
            {
                // Eski usul: her bağlantıya bir thread, thread Receive içinde bloklanır
                Thread t = new(() => server.Receive(buffer)) { IsBackground = true };
                t.Start();
                threads.Add(t);
            }
            else
            {
                // await: bekleyen bir thread yok, yalnız epoll'a kayıtlı bir socket var
                waits.Add(server.ReceiveAsync(buffer));
            }
        }

        await Task.Delay(500);
        int osThreads = OsThreadCount();
        double elapsed = sw.Elapsed.TotalMilliseconds;

        foreach (Socket client in clients)
        {
            client.Send([1]);
        }
        await Task.WhenAll(waits);
        threads.ForEach(t => t.Join());
        foreach (Socket s in clients.Concat(servers))
        {
            s.Dispose();
        }
        return new ConnectionReport(count, blocking ? "blocking" : "async", osThreads, elapsed);
    }

    public static async Task<IEnumerable<string>> UnixSocketRoundTripAsync(string path)
    {
        List<string> lines = [];
        // Process çökerse socket dosyası diskte kalır: bind'dan önce eskisini sil
        File.Delete(path);
        using (Socket listener = new(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
        {
            listener.Bind(new UnixDomainSocketEndPoint(path));
            listener.Listen();
            lines.Add($"bound {path}, file exists: {File.Exists(path)}");

            Task server = Task.Run(async () =>
            {
                using Socket connection = await listener.AcceptAsync();
                byte[] request = new byte[64];
                int read = await connection.ReceiveAsync(request);
                string isbn = Encoding.UTF8.GetString(request, 0, read);
                await connection.SendAsync(Encoding.UTF8.GetBytes($"stock {isbn} = 12"));
            });

            using Socket client = new(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            await client.ConnectAsync(new UnixDomainSocketEndPoint(path));
            await client.SendAsync("9780441172719"u8.ToArray());
            byte[] reply = new byte[64];
            int length = await client.ReceiveAsync(reply);
            await server;
            lines.Add($"reply: {Encoding.UTF8.GetString(reply, 0, length)}");
        }
        lines.Add($"after Dispose, file exists: {File.Exists(path)}");
        return lines;
    }

    public static async Task<IEnumerable<string>> NamedPipeRoundTripAsync(string name)
    {
        await using NamedPipeServerStream server = new(name, PipeDirection.InOut);
        await using NamedPipeClientStream client = new(".", name, PipeDirection.InOut);
        await Task.WhenAll(server.WaitForConnectionAsync(), client.ConnectAsync());

        await client.WriteAsync("report:daily"u8.ToArray());
        byte[] buffer = new byte[64];
        int read = await server.ReadAsync(buffer);

        List<string> lines = [$"server received: {Encoding.UTF8.GetString(buffer, 0, read)}"];
        if (!OperatingSystem.IsWindows())
        {
            // Unix'te named pipe aslında temp klasöründeki bir Unix domain socket'tir
            lines.AddRange(Directory.GetFileSystemEntries(Path.GetTempPath(), "CoreFxPipe_*"));
        }
        return lines;
    }

    public static async Task<int> WaitForShutdownAsync(TimeSpan cleanup)
    {
        Stopwatch sw = Stopwatch.StartNew();
        TaskCompletionSource<PosixSignal> stop = new(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnSignal(PosixSignalContext context)
        {
            // Varsayılan davranışı (process'i hemen bitirmek) iptal et, kapanışı biz yönetelim
            context.Cancel = true;
            stop.TrySetResult(context.Signal);
        }
        using PosixSignalRegistration term = PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnSignal);
        using PosixSignalRegistration interrupt = PosixSignalRegistration.Create(PosixSignal.SIGINT, OnSignal);

        Console.WriteLine($"pid={Environment.ProcessId} waiting for SIGTERM or SIGINT");
        PosixSignal signal = await stop.Task;
        Console.WriteLine($"[{sw.Elapsed.TotalSeconds,5:F1} s] {signal} received, finishing open orders");
        await Task.Delay(cleanup);
        Console.WriteLine($"[{sw.Elapsed.TotalSeconds,5:F1} s] shutdown complete");
        return 0;
    }
}
