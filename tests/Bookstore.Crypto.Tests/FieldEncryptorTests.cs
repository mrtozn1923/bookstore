using System.Security.Cryptography;
using Bookstore.Crypto.Fields;

namespace Bookstore.Crypto.Tests;

public class FieldEncryptorTests
{
    private static readonly Dictionary<byte, byte[]> Keys = new()
    {
        [1] = RandomNumberGenerator.GetBytes(32),
        [2] = RandomNumberGenerator.GetBytes(32),
    };

    private static readonly byte[] Phone = "+905550100001"u8.ToArray();
    private static readonly byte[] Ada = Guid.CreateVersion7().ToByteArray();

    private static FieldEncryptor Encryptor(byte active) => new(new FieldKeyRing(active, Keys));

    [Fact]
    public void Round_trip_with_the_same_associated_data() =>
        Assert.Equal(Phone, Encryptor(1).Decrypt(Encryptor(1).Encrypt(Phone, Ada), Ada));

    [Fact]
    public void Same_plaintext_never_gives_the_same_payload() =>
        Assert.NotEqual(Encryptor(1).Encrypt(Phone, Ada), Encryptor(1).Encrypt(Phone, Ada));

    [Fact]
    public void Other_associated_data_is_rejected()
    {
        byte[] payload = Encryptor(1).Encrypt(Phone, Ada);
        Assert.Throws<AuthenticationTagMismatchException>(() => Encryptor(1).Decrypt(payload, Guid.CreateVersion7().ToByteArray()));
    }

    [Theory]
    [InlineData(1)]   // keyId in the header
    [InlineData(5)]   // nonce
    [InlineData(20)]  // ciphertext
    [InlineData(42)]  // tag
    public void Any_changed_byte_is_rejected(int index)
    {
        byte[] payload = Encryptor(1).Encrypt(Phone, Ada);
        payload[index] ^= index == 1 ? (byte)0x03 : (byte)0x01;   // keyId 1 -> 2, a key that exists
        Assert.Throws<AuthenticationTagMismatchException>(() => Encryptor(1).Decrypt(payload, Ada));
    }

    [Fact]
    public void Old_payloads_stay_readable_after_rotation()
    {
        byte[] old = Encryptor(1).Encrypt(Phone, Ada);
        byte[] current = Encryptor(2).Encrypt(Phone, Ada);

        Assert.Equal(2, current[1]);
        Assert.Equal(Phone, Encryptor(2).Decrypt(old, Ada));
    }

    [Fact]
    public void Unknown_format_version_is_rejected()
    {
        byte[] payload = Encryptor(1).Encrypt(Phone, Ada);
        payload[0] = 9;
        Assert.Throws<CryptographicException>(() => Encryptor(1).Decrypt(payload, Ada));
    }

    [Fact]
    public void Wrapped_data_key_survives_kek_rotation_and_only_opens_with_the_current_kek()
    {
        byte[] oldKek = RandomNumberGenerator.GetBytes(32);
        byte[] newKek = RandomNumberGenerator.GetBytes(32);
        WrappedDataKey wrapped = EnvelopeKeys.CreateDataKey(3, "kek-old", oldKek);
        byte[] dek = EnvelopeKeys.Unwrap(wrapped, oldKek);

        WrappedDataKey rewrapped = EnvelopeKeys.Rewrap(wrapped, oldKek, "kek-new", newKek);

        Assert.Equal(40, rewrapped.WrappedKey.Length);
        Assert.Equal(dek, EnvelopeKeys.Unwrap(rewrapped, newKek));
        Assert.ThrowsAny<CryptographicException>(() => EnvelopeKeys.Unwrap(rewrapped, oldKek));
    }

    [Fact]
    public void Blind_index_is_deterministic_and_keyed()
    {
        BlindIndex a = new(RandomNumberGenerator.GetBytes(32));
        BlindIndex b = new(RandomNumberGenerator.GetBytes(32));

        Assert.Equal(a.Compute("+905550100001"), a.Compute("+905550100001"));
        Assert.NotEqual(a.Compute("+905550100001"), b.Compute("+905550100001"));
        Assert.NotEqual(a.Compute("+905550100001"), a.Compute("+905550100002"));
    }
}
