using Npgsql;
using NpgsqlTypes;

namespace Bookstore.ScaleLab.ReadRouting;

public sealed record ReplicaState(bool Reachable, NpgsqlLogSequenceNumber ReplayLsn, TimeSpan Lag, DateTime SampledAt)
{
    public static readonly ReplicaState Unknown = new(false, default, TimeSpan.MaxValue, DateTime.MinValue);
}

// asks the standby every interval how far it has replayed and how far behind it is
public sealed class ReplicaLagMonitor(NpgsqlDataSource standbyOnly, TimeSpan interval)
{
    const string Sql = """
        SELECT pg_last_wal_replay_lsn(),
               CASE WHEN pg_last_wal_receive_lsn() = pg_last_wal_replay_lsn() THEN 0
                    ELSE coalesce(extract(epoch FROM now() - pg_last_xact_replay_timestamp()), 0)
               END::float8
        """;

    public ReplicaState Current { get; private set; } = ReplicaState.Unknown;

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using NpgsqlConnection connection = await standbyOnly.OpenConnectionAsync(ct);
                await using NpgsqlCommand command = new(Sql, connection);
                await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);
                await reader.ReadAsync(ct);
                Current = new ReplicaState(true, reader.GetFieldValue<NpgsqlLogSequenceNumber>(0),
                    TimeSpan.FromSeconds(reader.GetDouble(1)), DateTime.UtcNow);
            }
            catch (NpgsqlException)
            {
                Current = ReplicaState.Unknown;
            }
            await Task.Delay(interval, ct).ContinueWith(_ => { }, CancellationToken.None);
        }
    }
}
