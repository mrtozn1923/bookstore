using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace Bookstore.ScaleLab.ReadRouting;

public sealed class ReadContextFactory(ReadRouter router, NpgsqlDataSource replicaSource, NpgsqlDataSource primarySource,
    bool retryOnFailure = false)
{
    readonly DbContextOptions<BookstoreReadContext> replicaOptions = Build(replicaSource, retryOnFailure);
    readonly DbContextOptions<BookstoreReadContext> primaryOptions = Build(primarySource, retryOnFailure);

    public BookstoreReadContext Create(NpgsqlLogSequenceNumber? lastWrite, out ReadTarget target)
    {
        target = router.Choose(lastWrite);
        return new BookstoreReadContext(target == ReadTarget.Replica ? replicaOptions : primaryOptions);
    }

    static DbContextOptions<BookstoreReadContext> Build(NpgsqlDataSource dataSource, bool retryOnFailure) =>
        new DbContextOptionsBuilder<BookstoreReadContext>()
            .UseNpgsql(dataSource, npgsql =>
            {
                if (retryOnFailure)
                {
                    npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(1), errorCodesToAdd: null);
                }
            })
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;
}
