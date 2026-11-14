using Npgsql;

namespace Bookstore.ScaleLab.Sharding;

public interface IShardResolver
{
    int ShardFor(int customerId);
    NpgsqlDataSource DataSourceFor(int customerId);
    IReadOnlyList<NpgsqlDataSource> AllShards { get; }
}

// customer -> one of 64 buckets (fixed, never changes) -> physical shard (a directory that can change)
public sealed class BucketShardResolver(IReadOnlyList<NpgsqlDataSource> shards, int[] bucketToShard) : IShardResolver
{
    public const int BucketCount = 64;

    // same formula as bucket_of() in SQL: Knuth's multiplicative hash, top 6 bits of 32
    public static int BucketOf(int customerId) => (int)((((ulong)(uint)customerId * 2654435761UL) % 4294967296UL) >> 26);

    public int ShardFor(int customerId) => bucketToShard[BucketOf(customerId)];

    public NpgsqlDataSource DataSourceFor(int customerId) => shards[ShardFor(customerId)];

    public IReadOnlyList<NpgsqlDataSource> AllShards => shards;
}
