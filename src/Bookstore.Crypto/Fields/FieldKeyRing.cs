namespace Bookstore.Crypto.Fields;

// Every key the application can decrypt with, and the one it encrypts with
public sealed class FieldKeyRing
{
    private readonly Dictionary<byte, byte[]> _keys;

    public FieldKeyRing(byte activeKeyId, IReadOnlyDictionary<byte, byte[]> keys)
    {
        if (!keys.ContainsKey(activeKeyId))
        {
            throw new ArgumentException($"Active key {activeKeyId} is not in the key ring.", nameof(activeKeyId));
        }

        if (keys.Values.Any(k => k.Length != 32))
        {
            throw new ArgumentException("Field keys must be 256 bits.", nameof(keys));
        }

        ActiveKeyId = activeKeyId;
        _keys = keys.ToDictionary(k => k.Key, k => k.Value.ToArray());
    }

    public byte ActiveKeyId { get; }

    public byte[] Get(byte keyId) =>
        _keys.TryGetValue(keyId, out byte[]? key)
            ? key
            : throw new KeyNotFoundException($"Field key {keyId} is not in the key ring.");
}

// One provider per process: rotation swaps the ring, the encryptor stays the same instance
public sealed class FieldKeyRingProvider(FieldKeyRing initial)
{
    private FieldKeyRing _current = initial;

    public FieldKeyRing Current => Volatile.Read(ref _current);

    public void Replace(FieldKeyRing keyRing) => Volatile.Write(ref _current, keyRing);
}
