using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace Bookstore.ScaleLab;

// Npgsql's strategy retries only errors it knows are transient. During a failover the
// multi-host data source can also report "No suitable host was found" for a few seconds;
// connection-level failures like that (not server errors) are worth retrying too.
public sealed class FailoverAwareRetryStrategy(ExecutionStrategyDependencies dependencies)
    : NpgsqlRetryingExecutionStrategy(dependencies, maxRetryCount: 10, maxRetryDelay: TimeSpan.FromSeconds(2), errorCodesToAdd: null)
{
    protected override bool ShouldRetryOn(Exception exception) =>
        base.ShouldRetryOn(exception) || exception is NpgsqlException and not PostgresException;
}
