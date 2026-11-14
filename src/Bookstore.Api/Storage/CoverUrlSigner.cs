using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace Bookstore.Storage;

// Signs short-lived read URLs with a user delegation key: no account key is involved anywhere.
public sealed class CoverUrlSigner(BlobServiceClient serviceClient, BlobContainerClient covers, TimeProvider clock)
{
    private static readonly TimeSpan KeyLifetime = TimeSpan.FromDays(1);
    private static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(15);
    private readonly SemaphoreSlim keyLock = new(1, 1);
    private UserDelegationKey? key;

    public async Task<SignedCoverUrl> SignAsync(string blobName, CancellationToken ct)
    {
        // Every request in the same clock hour gets the same URL (valid until the end of the next hour), so it can be cached
        DateTimeOffset now = clock.GetUtcNow();
        DateTimeOffset hourStart = new(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero);
        DateTimeOffset expiresOn = hourStart.AddHours(2);

        UserDelegationKey delegationKey = await GetKeyAsync(now, expiresOn, ct);
        BlobSasBuilder sas = new(BlobSasPermissions.Read, expiresOn)
        {
            BlobContainerName = covers.Name,
            BlobName = blobName,
            Resource = "b",
            StartsOn = hourStart - ClockSkew,
            Protocol = SasProtocol.Https,
        };
        Uri url = covers.GetBlobClient(blobName).GenerateUserDelegationSasUri(sas, delegationKey);
        return new SignedCoverUrl(url, expiresOn);
    }

    private async Task<UserDelegationKey> GetKeyAsync(DateTimeOffset now, DateTimeOffset neededUntil, CancellationToken ct)
    {
        await keyLock.WaitAsync(ct);
        try
        {
            // A SAS stops working when its delegation key expires, so the key must outlive the SAS
            if (key is null || key.SignedExpiresOn < neededUntil)
            {
                BlobGetUserDelegationKeyOptions options = new(now + KeyLifetime) { StartsOn = now - ClockSkew };
                key = (await serviceClient.GetUserDelegationKeyAsync(options, ct)).Value;
            }
            return key;
        }
        finally
        {
            keyLock.Release();
        }
    }
}

public sealed record SignedCoverUrl(Uri Url, DateTimeOffset ExpiresOn);
