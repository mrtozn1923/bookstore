namespace Bookstore.Shipping;

public sealed class ShippingOptions
{
    public const string SectionName = "Shipping";

    public Uri? BaseAddress { get; set; }

    public string ApiKey { get; set; } = "";

    // One attempt may take this long before the resilience pipeline cancels it and retries.
    public TimeSpan AttemptTimeout { get; set; } = TimeSpan.FromSeconds(2);

    public int MaxRetryAttempts { get; set; } = 2;

    // Development only: PEM files with extra root certificates that this client alone trusts
    // (the fake carrier's own certificate, mitmproxy's CA). Never installed into the OS trust store.
    public List<string> ExtraRootCertificates { get; set; } = [];
}
