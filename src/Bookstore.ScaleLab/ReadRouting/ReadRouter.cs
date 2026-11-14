using NpgsqlTypes;

namespace Bookstore.ScaleLab.ReadRouting;

public enum ReadTarget { Replica, Primary }

public sealed class ReadRouter(ReplicaLagMonitor monitor, TimeSpan maxLag, TimeSpan maxSampleAge)
{
    // lastWrite: the WAL position of this user's last write, if any (kept in a short-lived cookie)
    public ReadTarget Choose(NpgsqlLogSequenceNumber? lastWrite)
    {
        ReplicaState replica = monitor.Current;
        if (!replica.Reachable || DateTime.UtcNow - replica.SampledAt > maxSampleAge)
        {
            return ReadTarget.Primary;
        }
        if (replica.Lag > maxLag)
        {
            return ReadTarget.Primary;
        }
        if (lastWrite is { } lsn && replica.ReplayLsn < lsn)
        {
            return ReadTarget.Primary;
        }
        return ReadTarget.Replica;
    }
}
