using Microsoft.Net.Http.Headers;

namespace Bookstore.Api.Concurrency;

public static class ETags
{
    public static string From(byte[] version) => $"\"{Convert.ToBase64String(version)}\"";

    // If-Match: none -> Missing, "*" -> Any, a single strong ETag -> Version, anything else -> Invalid
    public static IfMatch ParseIfMatch(HttpRequest request)
    {
        IList<EntityTagHeaderValue> tags = request.GetTypedHeaders().IfMatch;
        if (tags.Count == 0)
        {
            return IfMatch.Missing;
        }
        if (tags.Count == 1 && tags[0].Equals(EntityTagHeaderValue.Any))
        {
            return IfMatch.Any;
        }
        if (tags.Count == 1 && !tags[0].IsWeak && TryDecode(tags[0].Tag.Value, out byte[] version))
        {
            return new IfMatch(IfMatchKind.Version, version);
        }
        return IfMatch.Invalid;
    }

    private static bool TryDecode(string? quoted, out byte[] version)
    {
        version = [];
        Span<byte> buffer = stackalloc byte[8];
        if (quoted is null || quoted.Length < 2 || !Convert.TryFromBase64String(quoted[1..^1], buffer, out int written) || written != 8)
        {
            return false;
        }
        version = buffer.ToArray();
        return true;
    }
}

public enum IfMatchKind { Missing, Any, Version, Invalid }

public sealed record IfMatch(IfMatchKind Kind, byte[]? Version = null)
{
    public static readonly IfMatch Missing = new(IfMatchKind.Missing);
    public static readonly IfMatch Any = new(IfMatchKind.Any);
    public static readonly IfMatch Invalid = new(IfMatchKind.Invalid);
}
