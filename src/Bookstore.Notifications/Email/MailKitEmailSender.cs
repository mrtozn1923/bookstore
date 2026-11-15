using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using MailKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Bookstore.Notifications.Email;

// One SMTP connection, reused across messages; SmtpClient is not thread-safe, so sends are serialized
public sealed class MailKitEmailSender(IOptions<SmtpOptions> options, ILogger<MailKitEmailSender> logger) : IEmailSender, IAsyncDisposable
{
    private readonly SmtpOptions _options = options.Value;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private SmtpClient? _client;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        MimeMessage mime = MimeMessageFactory.Create(message, _options);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            SmtpClient client = await GetConnectedClientAsync(cancellationToken);
            SendResult result = await client.SendAsync(mime, cancellationToken);
            logger.LogInformation("Sent {MessageId} to {To}: {Response}", mime.MessageId, message.To.Address, result.ResponseText);
        }
        catch (SmtpCommandException ex)
        {
            // 4xx: try again later; 5xx: the server will never accept this message
            bool transient = (int)ex.StatusCode is >= 400 and < 500;
            throw new EmailDeliveryException($"SMTP {(int)ex.StatusCode} {ex.ErrorCode}: {ex.Message}", transient, ex);
        }
        catch (Exception ex) when (ex is IOException or SmtpProtocolException or ServiceNotConnectedException or TimeoutException)
        {
            // The connection is in an unknown state: drop it so the next send reconnects
            await ResetClientAsync();
            throw new EmailDeliveryException($"SMTP connection failed: {ex.Message}", isTransient: true, ex);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<SmtpClient> GetConnectedClientAsync(CancellationToken cancellationToken)
    {
        if (_client is { IsConnected: true })
        {
            return _client;
        }

        await ResetClientAsync();
        SmtpClient client = new() { Timeout = (int)_options.Timeout.TotalMilliseconds };
        if (_options.TrustedCertificateSha256 is { } pinned)
        {
            client.ServerCertificateValidationCallback = (_, certificate, _, errors) =>
                errors == SslPolicyErrors.None || IsPinned(certificate, pinned);
            client.CheckCertificateRevocation = false;
        }

        await client.ConnectAsync(_options.Host, _options.Port, _options.Security, cancellationToken);
        if (_options.UserName is not null)
        {
            await client.AuthenticateAsync(_options.UserName, _options.Password ?? "", cancellationToken);
        }

        logger.LogInformation("Connected to {Host}:{Port} ({Security}), TLS {Tls}",
            _options.Host, _options.Port, _options.Security, client.SslProtocol);
        _client = client;
        return client;
    }

    private static bool IsPinned(X509Certificate? certificate, string expectedSha256) =>
        certificate is not null &&
        string.Equals(Convert.ToHexString(SHA256.HashData(certificate.GetRawCertData())), expectedSha256.Replace(":", ""), StringComparison.OrdinalIgnoreCase);

    private async Task ResetClientAsync()
    {
        if (_client is null)
        {
            return;
        }

        try
        {
            if (_client.IsConnected)
            {
                await _client.DisconnectAsync(quit: true);
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Ignoring error while closing the SMTP connection");
        }

        _client.Dispose();
        _client = null;
    }

    public async ValueTask DisposeAsync()
    {
        await ResetClientAsync();
        _gate.Dispose();
    }
}
