using System.Buffers.Text;
using System.Security.Cryptography;

namespace Bookstore.Notifications.Api;

// a P-256 key pair in the base64url form browsers and Lib.Net.Http.WebPush expect
public sealed record VapidKeys(string PublicKey, string PrivateKey)
{
    public static VapidKeys Generate()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        ECParameters p = key.ExportParameters(includePrivateParameters: true);
        byte[] publicKey = [0x04, .. p.Q.X!, .. p.Q.Y!]; // uncompressed point, 65 bytes
        return new VapidKeys(Base64Url.EncodeToString(publicKey), Base64Url.EncodeToString(p.D!));
    }
}
