namespace Bookstore.Invoicing;

public enum EInvoiceSecurity
{
    // Plain HTTP, only for local tests against a fake.
    None,
    // HTTPS + WS-Security UsernameToken in the SOAP header.
    UserName,
    // HTTPS + client certificate (mutual TLS).
    Certificate
}

public sealed class EInvoiceIntegratorOptions
{
    public required Uri Endpoint { get; set; }

    public EInvoiceSecurity Security { get; set; } = EInvoiceSecurity.UserName;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public string? ClientCertificatePath { get; set; }

    // Development only: trust exactly this self-signed certificate instead of the machine's trust store.
    public string? TrustedServerCertificatePath { get; set; }

    // WCF's own timeout for one call (send + wait for the reply).
    public TimeSpan SendTimeout { get; set; } = TimeSpan.FromSeconds(10);

    public required string SenderTaxNumber { get; set; }

    public bool LogMessages { get; set; }
}
