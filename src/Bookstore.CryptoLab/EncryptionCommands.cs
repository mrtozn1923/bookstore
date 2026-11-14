using System.Security.Cryptography;
using System.Text;
using Bookstore.Crypto.Fields;
using Bookstore.Crypto.Keys;
using Bookstore.Customers;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.CryptoLab;

public static class EncryptionCommands
{
    public static void Modes()
    {
        // Four identical 16-byte blocks
        byte[] plaintext = Encoding.ASCII.GetBytes(string.Concat(Enumerable.Repeat("ISBN 9780441013", 4).Select(s => s + "|")));
        byte[] key = RandomNumberGenerator.GetBytes(32);
        using Aes aes = Aes.Create();
        aes.Key = key;

        Print("ECB", aes.EncryptEcb(plaintext, PaddingMode.None));
        Print("CBC", aes.EncryptCbc(plaintext, RandomNumberGenerator.GetBytes(16), PaddingMode.None));

        using AesGcm gcm = new(key, tagSizeInBytes: 16);
        byte[] ciphertext = new byte[plaintext.Length];
        gcm.Encrypt(RandomNumberGenerator.GetBytes(12), plaintext, ciphertext, new byte[16]);
        Print("GCM", ciphertext);

        static void Print(string mode, byte[] data) =>
            Console.WriteLine($"{mode}: " + string.Join(" ", data.Chunk(16).Select(b => Convert.ToHexStringLower(b)[..8])));
    }

    public static void Aead()
    {
        byte[] key = new KeyDerivation(DevKeys.MasterKey).DeriveKey(KeyPurposes.CustomerPhoneEncryption);
        FieldEncryptor encryptor = new(new FieldKeyRing(1, new Dictionary<byte, byte[]> { [1] = key }));
        Guid ada = Guid.Parse("0197f001-0000-7000-8000-00000000c001");
        Guid alan = Guid.Parse("0197f001-0000-7000-8000-00000000c002");

        byte[] phone = "+905550100001"u8.ToArray();
        byte[] payload = encryptor.Encrypt(phone, ada.ToByteArray());
        Console.WriteLine($"plaintext {phone.Length} bytes -> payload {payload.Length} bytes (2 header + 12 nonce + {phone.Length} ciphertext + 16 tag)");
        Console.WriteLine($"same input again, different payload: {!payload.AsSpan().SequenceEqual(encryptor.Encrypt(phone, ada.ToByteArray()))}");
        Console.WriteLine($"decrypted for Ada: {Encoding.UTF8.GetString(encryptor.Decrypt(payload, ada.ToByteArray()))}");

        Try("moved to Alan's row", () => encryptor.Decrypt(payload, alan.ToByteArray()));
        byte[] tampered = payload.ToArray();
        tampered[20] ^= 0x01;
        Try("one ciphertext bit flipped", () => encryptor.Decrypt(tampered, ada.ToByteArray()));
        byte[] relabeled = payload.ToArray();
        relabeled[1] = 2;
        Try("keyId byte changed", () => encryptor.Decrypt(relabeled, ada.ToByteArray()));

        static void Try(string label, Func<byte[]> action)
        {
            try
            {
                action();
                Console.WriteLine($"{label}: decrypted (!)");
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine($"{label}: {ex.GetType().Name}");
            }
            catch (KeyNotFoundException ex)
            {
                Console.WriteLine($"{label}: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    public static void ChaCha()
    {
        if (!ChaCha20Poly1305.IsSupported)
        {
            Console.WriteLine("ChaCha20-Poly1305 is not supported on this platform");
            return;
        }

        byte[] key = RandomNumberGenerator.GetBytes(32);
        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] plaintext = "+905550100001"u8.ToArray();
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[16];

        using ChaCha20Poly1305 chacha = new(key);
        chacha.Encrypt(nonce, plaintext, ciphertext, tag, "Customer.Phone"u8);

        byte[] roundTrip = new byte[ciphertext.Length];
        chacha.Decrypt(nonce, ciphertext, tag, roundTrip, "Customer.Phone"u8);
        Console.WriteLine($"ChaCha20-Poly1305: {ciphertext.Length} + 16 tag bytes, round trip: {Encoding.UTF8.GetString(roundTrip)}");
    }

    public static void Envelope()
    {
        // Stand-in for a KEK held by Key Vault or an HSM
        byte[] kek2026 = new KeyDerivation(DevKeys.MasterKey).DeriveKey("bookstore/kek/2026");
        byte[] kek2027 = new KeyDerivation(DevKeys.MasterKey).DeriveKey("bookstore/kek/2027");

        WrappedDataKey dataKey = EnvelopeKeys.CreateDataKey(keyId: 2, kekId: "kek-2026", kek2026);
        Console.WriteLine($"DEK {dataKey.KeyId} wrapped by {dataKey.KekId}: {dataKey.WrappedKey.Length} bytes {Convert.ToHexStringLower(dataKey.WrappedKey)[..16]}...");

        byte[] dek = EnvelopeKeys.Unwrap(dataKey, kek2026);
        WrappedDataKey rewrapped = EnvelopeKeys.Rewrap(dataKey, kek2026, "kek-2027", kek2027);
        Console.WriteLine($"DEK {rewrapped.KeyId} wrapped by {rewrapped.KekId}: {rewrapped.WrappedKey.Length} bytes {Convert.ToHexStringLower(rewrapped.WrappedKey)[..16]}...");
        Console.WriteLine($"same DEK after KEK rotation: {EnvelopeKeys.Unwrap(rewrapped, kek2027).AsSpan().SequenceEqual(dek)}");

        try
        {
            EnvelopeKeys.Unwrap(rewrapped, kek2026);
        }
        catch (CryptographicException ex)
        {
            Console.WriteLine($"unwrap with the retired KEK: {ex.GetType().Name}");
        }
    }

    public static async Task CustomersAsync()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), "bookstore-customers.db");
        File.Delete(dbPath);
        KeyDerivation kdf = new(DevKeys.MasterKey);
        BlindIndex phoneIndex = new(kdf.DeriveKey(KeyPurposes.CustomerPhoneBlindIndex));

        // Key 1: derived with HKDF (previous post). Key 2: a random DEK wrapped by the KEK
        byte[] kek = kdf.DeriveKey("bookstore/kek/2026");
        WrappedDataKey dek2 = EnvelopeKeys.CreateDataKey(2, "kek-2026", kek);
        Dictionary<byte, byte[]> keys = new()
        {
            [1] = kdf.DeriveKey(KeyPurposes.CustomerPhoneEncryption),
            [2] = EnvelopeKeys.Unwrap(dek2, kek),
        };

        FieldKeyRingProvider ringProvider = new(new FieldKeyRing(activeKeyId: 1, keys));
        FieldEncryptor encryptor = new(ringProvider);

        await using (BookstoreContext db = Create())
        {
            await db.Database.EnsureCreatedAsync();
            Customer ada = new() { Name = "Ada" };
            ada.SetPhone("+90 (555) 010-00-01", phoneIndex);
            Customer alan = new() { Name = "Alan" };
            alan.SetPhone("+90 555 010 00 02", phoneIndex);
            db.Customers.AddRange(ada, alan);
            await db.SaveChangesAsync();

            await PrintRawAsync(db);
            string wanted = phoneIndex.Compute(PhoneNumber.Normalize("0090 555 010 0001"));
            Customer found = await db.Customers.SingleAsync(c => c.PhoneIndex == wanted);
            Console.WriteLine($"lookup by phone -> {found.Name} {found.Phone}");
        }

        // Rotation: key 2 becomes active; old rows stay readable, then get re-encrypted
        ringProvider.Replace(new FieldKeyRing(activeKeyId: 2, keys));
        await using (BookstoreContext db = Create())
        {
            Customer grace = new() { Name = "Grace" };
            grace.SetPhone("+90 555 010 00 03", phoneIndex);
            db.Customers.Add(grace);
            await db.SaveChangesAsync();
            await PrintKeyIdsAsync(db, "after adding Grace");

            foreach (Customer customer in await db.Customers.ToListAsync())
            {
                db.Entry(customer).Property(c => c.Phone).IsModified = true;
            }

            await db.SaveChangesAsync();
            await PrintKeyIdsAsync(db, "after re-encryption");
        }

        File.Delete(dbPath);

        BookstoreContext Create() =>
            new(new DbContextOptionsBuilder<BookstoreContext>().UseSqlite($"Data Source={dbPath};Pooling=False").Options, encryptor);
    }

    private static async Task PrintRawAsync(BookstoreContext db)
    {
        List<RawCustomer> rows = await db.Database
            .SqlQuery<RawCustomer>($"SELECT Name, Phone, PhoneIndex FROM Customers ORDER BY Name")
            .ToListAsync();
        foreach (RawCustomer row in rows)
        {
            Console.WriteLine($"{row.Name,-5} Phone={row.Phone[..24]}... ({row.Phone.Length} chars) PhoneIndex={row.PhoneIndex}");
        }
    }

    private static async Task PrintKeyIdsAsync(BookstoreContext db, string label)
    {
        List<RawCustomer> rows = await db.Database
            .SqlQuery<RawCustomer>($"SELECT Name, Phone, PhoneIndex FROM Customers ORDER BY Name")
            .ToListAsync();
        Console.WriteLine($"{label}: " + string.Join(", ", rows.Select(r => $"{r.Name}=key {FieldEncryptor.KeyIdOf(r.Phone)}")));
    }

    private sealed record RawCustomer(string Name, string Phone, string PhoneIndex);
}
