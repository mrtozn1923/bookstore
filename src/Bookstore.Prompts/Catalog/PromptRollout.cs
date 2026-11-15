using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Bookstore.Prompts.Catalog;

// Opens a new prompt version to a fixed share of customers; the same customer always gets the same version
public sealed class PromptRollout(string task, string stableVersion, string candidateVersion, int candidatePercent)
{
    public string Task { get; } = task;

    public int Bucket(string customerId) =>
        (int)(BinaryPrimitives.ReadUInt32BigEndian(SHA256.HashData(Encoding.UTF8.GetBytes($"{Task}:{customerId}"))) % 100);

    public string VersionFor(string customerId) =>
        Bucket(customerId) < candidatePercent ? candidateVersion : stableVersion;
}
