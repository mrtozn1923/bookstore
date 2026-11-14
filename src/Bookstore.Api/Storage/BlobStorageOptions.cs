namespace Bookstore.Storage;

public sealed class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    // Azure: https://<account>.blob.core.windows.net, used with a managed identity
    public Uri? ServiceUri { get; init; }

    // Client ID of the user-assigned managed identity; empty means system-assigned
    public string? ManagedIdentityClientId { get; init; }

    // Local development only (Azurite); never set in Azure
    public string? ConnectionString { get; init; }

    public string CoversContainer { get; init; } = "covers";
}
