using Microsoft.Extensions.Options;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace Bookstore.FileDrops;

public sealed class SftpDropOptions
{
    public required string Host { get; set; }
    public int Port { get; set; } = 22;
    public required string UserName { get; set; }
    public required string PrivateKeyPath { get; set; }
    // "SHA256:..." as printed by ssh-keygen -lf; the server must present exactly this key.
    public required string HostKeyFingerprint { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);
}

public sealed class SftpFileDrop(IOptionsMonitor<SftpDropOptions> options, string name, ILogger<SftpFileDrop> logger) : IFileDrop
{
    public async Task<IFileDropSession> OpenAsync(CancellationToken cancellationToken)
    {
        var settings = options.Get(name);
        using var key = new PrivateKeyFile(settings.PrivateKeyPath);
        var client = new SftpClient(new Renci.SshNet.ConnectionInfo(settings.Host, settings.Port, settings.UserName, new PrivateKeyAuthenticationMethod(settings.UserName, key))
        {
            Timeout = settings.Timeout
        });
        client.OperationTimeout = settings.Timeout;
        // Host key check: without it anyone between us and the supplier can pose as the server.
        client.HostKeyReceived += (_, e) =>
        {
            e.CanTrust = string.Equals("SHA256:" + e.FingerPrintSHA256, settings.HostKeyFingerprint, StringComparison.Ordinal);
            if (!e.CanTrust)
            {
                logger.LogError("Host key {Algorithm} SHA256:{Fingerprint} of {Drop} does not match the configured fingerprint",
                    e.HostKeyName, e.FingerPrintSHA256, name);
            }
        };

        try
        {
            await client.ConnectAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is SshException or System.Net.Sockets.SocketException)
        {
            client.Dispose();
            throw new FileDropException($"Could not connect to the '{name}' SFTP drop.", ex);
        }

        return new Session(client);
    }

    private sealed class Session(SftpClient client) : IFileDropSession
    {
        public async Task<IReadOnlyList<RemoteFile>> ListAsync(string folder, CancellationToken cancellationToken)
        {
            var files = new List<RemoteFile>();
            await foreach (var entry in client.ListDirectoryAsync(folder, cancellationToken))
            {
                if (entry.IsRegularFile)
                {
                    files.Add(new RemoteFile(entry.Name, entry.Length, new DateTimeOffset(entry.LastWriteTimeUtc, TimeSpan.Zero)));
                }
            }

            return files;
        }

        public Task DownloadAsync(string path, Stream destination, CancellationToken cancellationToken) =>
            client.DownloadFileAsync(path, destination, cancellationToken);

        public async Task UploadAsync(string path, Stream content, CancellationToken cancellationToken)
        {
            var temporary = path + ".part";
            await client.UploadFileAsync(content, temporary, cancellationToken);
            // SFTP rename fails if the target exists, so remove an earlier version first.
            if (await client.ExistsAsync(path, cancellationToken))
            {
                await client.DeleteFileAsync(path, cancellationToken);
            }

            await client.RenameFileAsync(temporary, path, cancellationToken);
        }

        public Task MoveAsync(string path, string newPath, CancellationToken cancellationToken) =>
            client.RenameFileAsync(path, newPath, cancellationToken);

        public ValueTask DisposeAsync()
        {
            client.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
