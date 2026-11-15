namespace Bookstore.Privacy.Protection;

// Keys come from a secret store (Key Vault, environment), never from the database or appsettings.json.
public sealed class PiiProtectionOptions
{
    public required string ActiveKeyId { get; init; }
    public required IReadOnlyDictionary<string, byte[]> EncryptionKeys { get; init; }
    public required byte[] BlindIndexKey { get; init; }
}
