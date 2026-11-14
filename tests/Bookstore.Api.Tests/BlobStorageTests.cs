using Azure.Identity;
using Azure.Storage.Blobs;
using Bookstore.Storage;

namespace Bookstore.Api.Tests;

public class BlobStorageTests
{
    private static readonly Uri ServiceUri = new("https://stbookstoredevweu.blob.core.windows.net");

    [Fact]
    public void Managed_identity_client_has_no_account_key()
    {
        BlobStorageOptions options = new() { ServiceUri = ServiceUri, ManagedIdentityClientId = "00000000-0000-0000-0000-000000000000" };

        BlobServiceClient client = BlobStorageExtensions.CreateServiceClient(options);

        Assert.IsType<ManagedIdentityCredential>(BlobStorageExtensions.CreateCredential(options));
        Assert.False(client.CanGenerateAccountSasUri);
    }

    [Fact]
    public void Connection_string_client_holds_the_account_key()
    {
        BlobStorageOptions options = new() { ConnectionString = "UseDevelopmentStorage=true" };

        BlobServiceClient client = BlobStorageExtensions.CreateServiceClient(options);

        Assert.True(client.CanGenerateAccountSasUri);
    }

    [Fact]
    public void Missing_service_uri_fails_with_a_clear_message()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => BlobStorageExtensions.CreateServiceClient(new BlobStorageOptions()));

        Assert.Contains("BlobStorage:ServiceUri", ex.Message);
    }
}
