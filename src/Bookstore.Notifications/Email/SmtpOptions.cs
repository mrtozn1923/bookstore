using System.ComponentModel.DataAnnotations;
using MailKit.Security;

namespace Bookstore.Notifications.Email;

public sealed class SmtpOptions
{
    [Required]
    public string Host { get; set; } = "";

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    // 587 -> StartTls, 465 -> SslOnConnect
    public SecureSocketOptions Security { get; set; } = SecureSocketOptions.StartTls;

    public string? UserName { get; set; }

    // Comes from an environment variable or a secret store, never from appsettings.json
    public string? Password { get; set; }

    [Required, EmailAddress]
    public string FromAddress { get; set; } = "";

    public string FromName { get; set; } = "Bookstore";

    // Development only: SHA-256 thumbprint of a self-signed test server certificate
    public string? TrustedCertificateSha256 { get; set; }

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
