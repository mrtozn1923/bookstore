using System.Buffers.Binary;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Bookstore.Crypto.Pki;
using Bookstore.Crypto.Tls;

namespace Bookstore.CryptoLab;

public static class TlsCommands
{
    private const string Dir = "certs";

    public static async Task HandshakeAsync()
    {
        await ShowAsync("OS defaults", SslProtocols.None, SslProtocols.None);
        await ShowAsync("both TLS 1.2", SslProtocols.Tls12, SslProtocols.Tls12);
        await ShowAsync("server 1.3, client 1.2", SslProtocols.Tls13, SslProtocols.Tls12);
        if (OperatingSystem.IsLinux())
        {
            await ShowAsync("server ChaCha20 only", SslProtocols.None, SslProtocols.None,
                new CipherSuitesPolicy([TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256]));
        }
    }

    // Development only: decrypts our own loopback connection with the secrets .NET writes to SSLKEYLOGFILE
    public static async Task KeyLogAsync()
    {
        string path = Environment.GetEnvironmentVariable("SSLKEYLOGFILE") ?? throw new InvalidOperationException("Set SSLKEYLOGFILE");
        // Release builds of .NET also need this switch; it is read once, at the first TLS connection
        AppContext.SetSwitch("System.Net.EnableSslKeyLogging", true);

        byte[] firstClientSecret = [];
        for (int connection = 1; connection <= 2; connection++)
        {
            WireTap tap = null!;
            await using LoopbackTls tls = await LoopbackTls.ConnectAsync(SslProtocols.None, SslProtocols.None, null, s => tap = new WireTap(s));
            byte[] request = """{"orderId":"0197f001-0000-7000-8000-000000000042","isbn":"9780441172719","quantity":2}"""u8.ToArray();
            await tls.Client.WriteAsync(request);
            byte[] buffer = new byte[256];
            int read = await tls.Server.ReadAsync(buffer);
            await tls.Server.WriteAsync("reserved"u8.ToArray());
            read = await tls.Client.ReadAsync(buffer);

            KeyLog log = KeyLog.Parse(await File.ReadAllTextAsync(path));
            List<TlsRecord> sent = TlsRecords.Parse(tap.Sent.ToArray());
            List<TlsRecord> received = TlsRecords.Parse(tap.Received.ToArray());
            byte[] clientRandom = sent[0].Fragment[6..38]; // handshake type(1) | length(3) | version(2) | random(32)
            byte[] clientSecret = log.Find("CLIENT_TRAFFIC_SECRET_0", clientRandom);
            if (connection == 2)
            {
                Console.WriteLine($"connection 2: client_random {Convert.ToHexStringLower(clientRandom)[..16]}..., CLIENT_TRAFFIC_SECRET_0 {Convert.ToHexStringLower(clientSecret)[..16]}...");
                Console.WriteLine($"same certificate, different traffic secret: {!clientSecret.AsSpan().SequenceEqual(firstClientSecret)}");
                break;
            }

            firstClientSecret = clientSecret;
            Console.WriteLine($"{tls.Client.SslProtocol}, {tls.Client.NegotiatedCipherSuite}");
            Console.WriteLine($"key log labels: {string.Join(", ", log.Entries.Select(e => e.Label).Distinct())}");
            Console.WriteLine($"connection 1: client_random {Convert.ToHexStringLower(clientRandom)[..16]}..., CLIENT_TRAFFIC_SECRET_0 {Convert.ToHexStringLower(clientSecret)[..16]}...");
            (HashAlgorithmName hash, int keyLength) = tls.Client.NegotiatedCipherSuite switch
            {
                TlsCipherSuite.TLS_AES_128_GCM_SHA256 => (HashAlgorithmName.SHA256, 16),
                TlsCipherSuite.TLS_AES_256_GCM_SHA384 => (HashAlgorithmName.SHA384, 32),
                TlsCipherSuite suite => throw new NotSupportedException($"{suite}: only AES-GCM suites are decoded here"),
            };

            Console.WriteLine("client -> server");
            Decode(sent, "CLIENT", log, clientRandom, hash, keyLength);
            Console.WriteLine("server -> client");
            Decode(received, "SERVER", log, clientRandom, hash, keyLength);
        }
    }

    // Plain SslStream echo server for openssl s_client (no client certificate)
    public static async Task ServeAsync(string[] args)
    {
        int port = args.Length > 1 ? int.Parse(args[1]) : 7444;
        using X509Certificate2 certificate = ServiceCertificates.LoadFromPem(Path.Combine(Dir, "api.crt.pem"), Path.Combine(Dir, "api.key.pem"));
        X509Certificate2Collection chain = ServiceCertificates.LoadAll(Path.Combine(Dir, "services-ca.crt.pem"));
        SslStreamCertificateContext context = SslStreamCertificateContext.Create(certificate, chain, offline: true);
        TcpListener listener = new(IPAddress.Loopback, port);
        listener.Start();
        Console.WriteLine($"listening on 127.0.0.1:{port}");
        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            _ = Task.Run(async () =>
            {
                using (client)
                await using (SslStream ssl = new(client.GetStream()))
                {
                    try
                    {
                        await ssl.AuthenticateAsServerAsync(new SslServerAuthenticationOptions { ServerCertificateContext = context });
                        byte[] buffer = new byte[256];
                        int read = await ssl.ReadAsync(buffer);
                        await ssl.WriteAsync(buffer.AsMemory(0, read));
                    }
                    catch (Exception ex) when (ex is AuthenticationException or IOException)
                    {
                        Console.WriteLine($"handshake failed: {ex.Message}");
                    }
                }
            });
        }
    }

    private static void Decode(List<TlsRecord> records, string side, KeyLog log, byte[] clientRandom, HashAlgorithmName hash, int keyLength)
    {
        TlsTrafficKeys handshakeKeys = TlsKeySchedule.TrafficKeys(hash, log.Find($"{side}_HANDSHAKE_TRAFFIC_SECRET", clientRandom), keyLength);
        TlsTrafficKeys applicationKeys = TlsKeySchedule.TrafficKeys(hash, log.Find($"{side}_TRAFFIC_SECRET_0", clientRandom), keyLength);
        string prefix = side == "CLIENT" ? "c" : "s";
        bool handshakePhase = true;
        ulong sequence = 0;
        foreach (TlsRecord record in records)
        {
            string head = $"  {record.Type,-16} {record.Fragment.Length,5} B";
            if (record.Type != TlsContentType.ApplicationData)
            {
                Console.WriteLine($"{head}  plaintext: {Describe(record.Type, record.Fragment)}");
                continue;
            }

            (TlsContentType inner, byte[] content) = TlsRecords.Decrypt(handshakePhase ? handshakeKeys : applicationKeys, sequence, record);
            Console.WriteLine($"{head}  {prefix} {(handshakePhase ? "hs" : "ap")} traffic, seq {sequence}: {inner} {Describe(inner, content)}");
            sequence++;
            if (handshakePhase && inner == TlsContentType.Handshake && HandshakeMessages(content).Contains("Finished"))
            {
                handshakePhase = false;
                sequence = 0; // new keys, new sequence
            }
        }
    }

    private static string Describe(TlsContentType type, byte[] content) => type switch
    {
        TlsContentType.Handshake => string.Join(", ", HandshakeMessages(content)),
        TlsContentType.ApplicationData => "\"" + Shorten(Encoding.UTF8.GetString(content)) + "\"",
        TlsContentType.ChangeCipherSpec => "(compatibility, ignored in TLS 1.3)",
        _ => Convert.ToHexString(content),
    };

    private static List<string> HandshakeMessages(ReadOnlySpan<byte> content)
    {
        List<string> names = [];
        while (content.Length >= 4)
        {
            int length = (content[1] << 16) | BinaryPrimitives.ReadUInt16BigEndian(content[2..4]);
            names.Add(content[0] switch
            {
                1 => "ClientHello", 2 => "ServerHello", 4 => "NewSessionTicket", 8 => "EncryptedExtensions",
                11 => "Certificate", 13 => "CertificateRequest", 15 => "CertificateVerify", 20 => "Finished",
                byte other => $"type {other}",
            });
            content = content[Math.Min(content.Length, 4 + length)..];
        }

        return names;
    }

    private static string Shorten(string s) => s.Length > 40 ? s[..40] + "..." : s;

    private static async Task ShowAsync(string label, SslProtocols server, SslProtocols client, CipherSuitesPolicy? policy = null)
    {
        try
        {
            await using LoopbackTls tls = await LoopbackTls.ConnectAsync(server, client, policy);
            Console.WriteLine($"{label,-24}: {tls.Client.SslProtocol}, {tls.Client.NegotiatedCipherSuite}");
        }
        catch (AuthenticationException ex)
        {
            Console.WriteLine($"{label,-24}: {ex.GetType().Name}: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    internal sealed class LoopbackTls : IAsyncDisposable
    {
        private readonly TcpClient _clientSocket;
        private readonly TcpClient _serverSocket;

        private LoopbackTls(TcpClient clientSocket, TcpClient serverSocket, SslStream client, SslStream server)
        {
            _clientSocket = clientSocket;
            _serverSocket = serverSocket;
            Client = client;
            Server = server;
        }

        public SslStream Client { get; }
        public SslStream Server { get; }

        public static async Task<LoopbackTls> ConnectAsync(SslProtocols server, SslProtocols client, CipherSuitesPolicy? policy,
            Func<Stream, Stream>? wrapClientStream = null)
        {
            using X509Certificate2 certificate = ServiceCertificates.LoadFromPem(Path.Combine(Dir, "api.crt.pem"), Path.Combine(Dir, "api.key.pem"));
            X509Certificate2Collection trustAnchors = ServiceCertificates.LoadAll(Path.Combine(Dir, "root-ca.crt.pem"));
            X509Certificate2Collection intermediates = ServiceCertificates.LoadAll(Path.Combine(Dir, "services-ca.crt.pem"));

            using TcpListener listener = new(IPAddress.Loopback, 0);
            listener.Start();
            TcpClient clientSocket = new();
            Task connect = clientSocket.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
            TcpClient serverSocket = await listener.AcceptTcpClientAsync();
            await connect;

            Stream clientStream = wrapClientStream?.Invoke(clientSocket.GetStream()) ?? clientSocket.GetStream();
            SslStream clientSsl = new(clientStream);
            SslStream serverSsl = new(serverSocket.GetStream());
            using SocketsHttpHandler handler = ServiceCertificates.CreateMtlsHandler(trustAnchors, null);
            SslClientAuthenticationOptions clientOptions = new()
            {
                TargetHost = "localhost",
                EnabledSslProtocols = client, // None = let the OS decide
                CertificateChainPolicy = handler.SslOptions.CertificateChainPolicy,
            };
            SslServerAuthenticationOptions serverOptions = new()
            {
                ServerCertificateContext = SslStreamCertificateContext.Create(certificate, intermediates, offline: true),
                EnabledSslProtocols = server,
                CipherSuitesPolicy = policy,
            };

            LoopbackTls tls = new(clientSocket, serverSocket, clientSsl, serverSsl);
            try
            {
                await Task.WhenAll(clientSsl.AuthenticateAsClientAsync(clientOptions), serverSsl.AuthenticateAsServerAsync(serverOptions));
                return tls;
            }
            catch
            {
                await tls.DisposeAsync();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await Client.DisposeAsync();
            await Server.DisposeAsync();
            _clientSocket.Dispose();
            _serverSocket.Dispose();
        }
    }

    // Records the raw bytes of a stream in both directions: what TLS puts on the wire
    internal sealed class WireTap(Stream inner) : Stream
    {
        public MemoryStream Sent { get; } = new();
        public MemoryStream Received { get; } = new();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int read = await inner.ReadAsync(buffer, cancellationToken);
            Received.Write(buffer.Span[..read]);
            return read;
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Sent.Write(buffer.Span);
            await inner.WriteAsync(buffer, cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
        public override void Write(byte[] buffer, int offset, int count) => WriteAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);
        public override void Flush() => inner.Flush();
        public override bool CanRead => true;
        public override bool CanWrite => true;
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
