using System.IO.Hashing;
using System.Text;

namespace Algorithms.Randomness;

// Deterministic bucketing: the same flag and user always land in the same bucket, on every server and every run
public static class Rollout
{
    public static int Bucket(string flag, string userId)
    {
        ulong hash = XxHash3.HashToUInt64(Encoding.UTF8.GetBytes($"{flag}:{userId}"));
        return (int)(hash % 100);
    }

    public static bool IsEnabled(string flag, string userId, int percentage) => Bucket(flag, userId) < percentage;
}
