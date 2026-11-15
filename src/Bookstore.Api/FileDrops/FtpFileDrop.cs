using System.Security.Cryptography.X509Certificates;
using FluentFTP;
using FluentFTP.Exceptions;
using Microsoft.Extensions.Options;

namespace Bookstore.FileDrops;

public sealed class FtpDropOptions
{
    public required string Host { get; set; }
    public int Port { get; set; } = 21;
    public required string UserName { get; set; }
    public required string Password { get; set; }
    // Explicit FTPS (AUTH TLS). Plain FTP sends the password in clear text.
    public bool UseTls { get; set; } = true;
    // Development only: trust exactly this self-signed certificate.
    public string? TrustedCertificatePath { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);
}

public sealed class FtpFileDrop(IOptionsMonitor<FtpDropOptions> options, string name) : IFileDrop
{
    public async Task<IFileDropSession> OpenAsync(CancellationToken cancellationToken)
    {
        var settings = options.Get(name);
        var client = new AsyncFtpClient(settings.Host, settings.UserName, settings.Password, settings.Port);
        client.Config.ConnectTimeout = (int)settings.Timeout.TotalMilliseconds;
        client.Config.ReadTimeout = (int)settings.Timeout.TotalMilliseconds;
        client.Config.DataConnectionType = FtpDataConnectionType.PASV;
        if (settings.UseTls)
        {
            client.Config.EncryptionMode = FtpEncryptionMode.Explicit;
            client.Config.DataConnectionEncryption = true;
            if (settings.TrustedCertificatePath is { Length: > 0 } path)
            {
                var trusted = X509CertificateLoader.LoadCertificateFromFile(path);
                client.ValidateCertificate += (_, e) => e.Accept = e.Certificate.GetRawCertData().AsSpan().SequenceEqual(trusted.RawData);
            }
        }

        try
        {
            await client.Connect(cancellationToken);
        }
        catch (Exception ex) when (ex is FtpException or System.Net.Sockets.SocketException or TimeoutException or System.Security.Authentication.AuthenticationException)
        {
            await client.DisposeAsync();
            throw new FileDropException($"Could not connect to the '{name}' FTP drop.", ex);
        }

        return new Session(client);
    }

    private sealed class Session(AsyncFtpClient client) : IFileDropSession
    {
        public async Task<IReadOnlyList<RemoteFile>> ListAsync(string folder, CancellationToken cancellationToken) =>
            [.. (await client.GetListing(folder, cancellationToken))
                .Where(item => item.Type == FtpObjectType.File)
                .Select(item => new RemoteFile(item.Name, item.Size, new DateTimeOffset(item.Modified, TimeSpan.Zero)))];

        public async Task DownloadAsync(string path, Stream destination, CancellationToken cancellationToken)
        {
            if (!await client.DownloadStream(destination, path, token: cancellationToken))
            {
                throw new FileDropException($"Download of {path} failed.");
            }
        }

        public async Task UploadAsync(string path, Stream content, CancellationToken cancellationToken)
        {
            var temporary = path + ".part";
            await client.UploadStream(content, temporary, FtpRemoteExists.Overwrite, token: cancellationToken);
            await client.MoveFile(temporary, path, FtpRemoteExists.Overwrite, cancellationToken);
        }

        public Task MoveAsync(string path, string newPath, CancellationToken cancellationToken) =>
            client.MoveFile(path, newPath, FtpRemoteExists.Overwrite, cancellationToken);

        public async ValueTask DisposeAsync()
        {
            await client.Disconnect(CancellationToken.None);
            await client.DisposeAsync();
        }
    }
}
