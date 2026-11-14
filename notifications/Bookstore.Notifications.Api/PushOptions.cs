using System.ComponentModel.DataAnnotations;

namespace Bookstore.Notifications.Api;

public sealed class PushOptions
{
    public const string Section = "Push";

    // never in appsettings.json: environment variables or a secret store (Push__PublicKey, Push__PrivateKey)
    [Required] public string PublicKey { get; set; } = "";
    [Required] public string PrivateKey { get; set; } = "";
    [Required] public string Subject { get; set; } = "";

    // push services we are willing to call; an exact host or a ".suffix"
    public string[] AllowedEndpointHosts { get; set; } = [];

    public bool IsAllowed(Uri endpoint) =>
        (endpoint.Scheme == Uri.UriSchemeHttps || endpoint.IsLoopback)
        && AllowedEndpointHosts.Any(h => h.StartsWith('.') ? endpoint.Host.EndsWith(h) : endpoint.Host == h);
}
