using System.Security.Cryptography;
using Bookstore.Crypto.Tls;

namespace Bookstore.Crypto.Tests;

// Test vectors: RFC 8448 "Example Handshake Traces for TLS 1.3", section 3 (TLS_AES_128_GCM_SHA256)
public class TlsKeyScheduleTests
{
    private static byte[] Hex(string hex) => Convert.FromHexString(hex);

    [Fact]
    public void Server_handshake_traffic_secret_matches_rfc8448()
    {
        byte[] handshakeSecret = Hex("1dc826e93606aa6fdc0aadc12f741b01046aa6b99f691ed221a9f0ca043fbeac");
        byte[] transcriptHash = Hex("860c06edc07858ee8e78f0e7428c58edd6b43f2ca3e6e95f02ed063cf0e1cad8");

        byte[] secret = TlsKeySchedule.DeriveSecret(HashAlgorithmName.SHA256, handshakeSecret, "s hs traffic", transcriptHash);

        Assert.Equal("b67b7d690cc16c4e75e54213cb2d37b4e9c912bcded9105d42befd59d391ad38", Convert.ToHexStringLower(secret));
    }

    [Theory]
    [InlineData("b67b7d690cc16c4e75e54213cb2d37b4e9c912bcded9105d42befd59d391ad38", "3fce516009c21727d0f2e4e86ee403bc", "5d313eb2671276ee13000b30")]
    [InlineData("9e40646ce79a7f9dc05af8889bce6552875afa0b06df0087f792ebb7c17504a5", "17422dda596ed5d9acd890e3c63f5051", "5b78923dee08579033e523d9")]
    public void Traffic_key_and_iv_match_rfc8448(string trafficSecret, string key, string iv)
    {
        TlsTrafficKeys keys = TlsKeySchedule.TrafficKeys(HashAlgorithmName.SHA256, Hex(trafficSecret), keyLength: 16);
        Assert.Equal(key, Convert.ToHexStringLower(keys.Key));
        Assert.Equal(iv, Convert.ToHexStringLower(keys.Iv));
    }

    private const string ClientApplicationRecord =
        "1703030043a23f7054b62c94d0affafe8228ba55cbefacea42f914aa66bcab3f2b9819a8a5b46b395bd54a9a20441e2b62974e1f5a6292a2977014bd1e3deae63aeebb21694915e4";

    private static readonly TlsTrafficKeys ClientApplicationKeys = new(Hex("17422dda596ed5d9acd890e3c63f5051"), Hex("5b78923dee08579033e523d9"));

    [Fact]
    public void Client_application_record_decrypts_to_the_rfc8448_payload()
    {
        TlsRecord record = Assert.Single(TlsRecords.Parse(Hex(ClientApplicationRecord)));

        (TlsContentType type, byte[] content) = TlsRecords.Decrypt(ClientApplicationKeys, sequenceNumber: 0, record);

        Assert.Equal(TlsContentType.ApplicationData, type);
        Assert.Equal(Enumerable.Range(0, 50).Select(i => (byte)i), content);
    }

    [Fact]
    public void Wrong_sequence_number_or_changed_header_fails_authentication()
    {
        TlsRecord record = TlsRecords.Parse(Hex(ClientApplicationRecord))[0];
        byte[] header = record.Header.ToArray();
        header[2] = 0x01; // legacy_version is part of the AAD

        Assert.ThrowsAny<CryptographicException>(() => TlsRecords.Decrypt(ClientApplicationKeys, 1, record));
        Assert.ThrowsAny<CryptographicException>(() => TlsRecords.Decrypt(ClientApplicationKeys, 0, record with { Header = header }));
    }

    [Fact]
    public void Key_log_lines_are_found_by_label_and_client_random()
    {
        KeyLog log = KeyLog.Parse("""
            # comment
            CLIENT_HANDSHAKE_TRAFFIC_SECRET 00aa 0102
            CLIENT_TRAFFIC_SECRET_0 00AA 0a0b
            """);

        Assert.Equal([0x0a, 0x0b], log.Find("CLIENT_TRAFFIC_SECRET_0", [0x00, 0xaa]));
    }
}
