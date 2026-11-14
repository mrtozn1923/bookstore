using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Bookstore.FakeProviders;

// A minimal SMTP server for local runs: accepts every message and prints its subject.
public sealed class FakeSmtpServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly FakeMode _mode;
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _loop;

    public FakeSmtpServer(int port, FakeMode mode)
    {
        _mode = mode;
        _listener = new TcpListener(IPAddress.Loopback, port);
        _listener.Start();
        _loop = AcceptLoopAsync();
    }

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    public int Messages { get; private set; }

    private async Task AcceptLoopAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(_stop.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _ = HandleAsync(client);
        }
    }

    private async Task HandleAsync(TcpClient client)
    {
        using var _ = client;
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII);
        await using var writer = new StreamWriter(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };

        if (_mode == FakeMode.Slow)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        if (_mode == FakeMode.Throttle)
        {
            await writer.WriteLineAsync("421 4.3.2 Service not available, try again later");
            return;
        }

        await writer.WriteLineAsync("220 localhost Fake SMTP ready");
        string? subject = null;
        while (await reader.ReadLineAsync() is { } line)
        {
            var command = line.Split(' ')[0].ToUpperInvariant();
            switch (command)
            {
                case "EHLO":
                case "HELO":
                    await writer.WriteLineAsync("250 localhost");
                    break;
                case "DATA":
                    await writer.WriteLineAsync("354 End data with <CR><LF>.<CR><LF>");
                    while (await reader.ReadLineAsync() is { } data && data != ".")
                    {
                        if (data.StartsWith("Subject:", StringComparison.OrdinalIgnoreCase))
                        {
                            subject = data["Subject:".Length..].Trim();
                        }
                    }

                    Messages++;
                    Console.WriteLine($"SMTP  : accepted message '{subject}'");
                    await writer.WriteLineAsync("250 2.0.0 OK queued");
                    break;
                case "QUIT":
                    await writer.WriteLineAsync("221 Bye");
                    return;
                default:
                    await writer.WriteLineAsync("250 OK");
                    break;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        _listener.Stop();
        await _loop;
    }
}
