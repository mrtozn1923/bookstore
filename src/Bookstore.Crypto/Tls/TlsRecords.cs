using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Bookstore.Crypto.Tls;

public enum TlsContentType : byte
{
    ChangeCipherSpec = 20,
    Alert = 21,
    Handshake = 22,
    ApplicationData = 23,
}

public sealed record TlsRecord(TlsContentType Type, byte[] Header, byte[] Fragment);

public static class TlsRecords
{
    // Splits a captured byte stream into records: type(1) | legacy_version(2) | length(2) | fragment
    public static List<TlsRecord> Parse(ReadOnlySpan<byte> wire)
    {
        List<TlsRecord> records = [];
        while (wire.Length >= 5)
        {
            int length = BinaryPrimitives.ReadUInt16BigEndian(wire[3..5]);
            if (wire.Length < 5 + length)
            {
                break;
            }

            records.Add(new TlsRecord((TlsContentType)wire[0], wire[..5].ToArray(), wire[5..(5 + length)].ToArray()));
            wire = wire[(5 + length)..];
        }

        return records;
    }

    // RFC 8446 5.2-5.3: nonce = iv XOR sequence number, AAD = record header,
    // plaintext = content | real content type | zero padding
    public static (TlsContentType Type, byte[] Content) Decrypt(TlsTrafficKeys keys, ulong sequenceNumber, TlsRecord record)
    {
        byte[] nonce = keys.Iv.ToArray();
        Span<byte> sequence = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(sequence, sequenceNumber);
        for (int i = 0; i < 8; i++)
        {
            nonce[nonce.Length - 8 + i] ^= sequence[i];
        }

        const int TagSize = 16;
        ReadOnlySpan<byte> fragment = record.Fragment;
        byte[] plaintext = new byte[fragment.Length - TagSize];
        using AesGcm aes = new(keys.Key, TagSize);
        aes.Decrypt(nonce, fragment[..^TagSize], fragment[^TagSize..], plaintext, record.Header);

        int end = plaintext.Length - 1;
        while (end >= 0 && plaintext[end] == 0)
        {
            end--;
        }

        return ((TlsContentType)plaintext[end], plaintext[..end]);
    }
}
