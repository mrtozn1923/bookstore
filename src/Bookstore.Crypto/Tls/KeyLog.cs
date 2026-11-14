namespace Bookstore.Crypto.Tls;

// SSLKEYLOGFILE format (RFC 9850): "<label> <client_random hex> <secret hex>" per line
public sealed class KeyLog(IReadOnlyList<(string Label, string ClientRandom, byte[] Secret)> entries)
{
    public IReadOnlyList<(string Label, string ClientRandom, byte[] Secret)> Entries { get; } = entries;

    public static KeyLog Parse(string text) => new(
        [.. text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !line.StartsWith('#'))
            .Select(line => line.Split(' '))
            .Where(parts => parts.Length == 3)
            .Select(parts => (parts[0], parts[1].ToLowerInvariant(), Convert.FromHexString(parts[2])))]);

    public byte[] Find(string label, ReadOnlySpan<byte> clientRandom)
    {
        string random = Convert.ToHexStringLower(clientRandom);
        return Entries.First(e => e.Label == label && e.ClientRandom == random).Secret;
    }
}
