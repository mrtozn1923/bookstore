using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace Bookstore.ScaleLab.ReadRouting;

public sealed class ReadContextFactory(ReadRouter router, NpgsqlDataSource replicaSource, NpgsqlDataSource primarySource)
{
    readonly DbContextOptions<BookstoreReadContext> replicaOptions = Build(replicaSource);
    readonly DbContextOptions<BookstoreReadContext> primaryOptions = Build(primarySource);

    public BookstoreReadContext Create(NpgsqlLogSequenceNumber? lastWrite, out ReadTarget target)
    {
        target = router.Choose(lastWrite);
        return new BookstoreReadContext(target == ReadTarget.Replica ? replicaOptions : primaryOptions);
    }

    static DbContextOptions<BookstoreReadContext> Build(NpgsqlDataSource dataSource) =>
        new DbContextOptionsBuilder<BookstoreReadContext>()
            .UseNpgsql(dataSource)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;
}
