using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;

namespace Bookstore.Storage;

public static class BlobStorageExtensions
{
    public static IServiceCollection AddBlobStorage(this IServiceCollection services, IConfiguration configuration)
    {
        BlobStorageOptions options = configuration.GetSection(BlobStorageOptions.SectionName).Get<BlobStorageOptions>() ?? new();
        services.AddSingleton(options);
        // Created on first use, so the API still starts when storage isn't configured
        services.AddSingleton(_ => CreateServiceClient(options));
        services.AddSingleton(sp => sp.GetRequiredService<BlobServiceClient>().GetBlobContainerClient(options.CoversContainer));
        services.AddSingleton<CoverUrlSigner>();
        return services;
    }

    public static BlobServiceClient CreateServiceClient(BlobStorageOptions options)
    {
        if (options.ConnectionString is { Length: > 0 } connectionString)
        {
            return new BlobServiceClient(connectionString);
        }

        Uri serviceUri = options.ServiceUri
            ?? throw new InvalidOperationException("BlobStorage:ServiceUri is missing (see README.md).");
        return new BlobServiceClient(serviceUri, CreateCredential(options));
    }

    // Azure: always the user-assigned identity, no fallback chain.
    // Developer machine: DefaultAzureCredential picks up the az login / Visual Studio account.
    public static TokenCredential CreateCredential(BlobStorageOptions options) =>
        options.ManagedIdentityClientId is { Length: > 0 } clientId
            ? new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(clientId))
            : new DefaultAzureCredential();
}
