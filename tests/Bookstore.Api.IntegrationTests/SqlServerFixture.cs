using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(Bookstore.Api.IntegrationTests.SqlServerFixture))]
[assembly: Trait("Category", "Integration")]

namespace Bookstore.Api.IntegrationTests;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest").Build();

    public TimeSpan StartupTime { get; private set; }

    public async ValueTask InitializeAsync()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        await container.StartAsync();
        StartupTime = stopwatch.Elapsed;
    }

    public string ConnectionStringFor(string database) =>
        new SqlConnectionStringBuilder(container.GetConnectionString()) { InitialCatalog = database }.ConnectionString;

    public async ValueTask DisposeAsync() => await container.DisposeAsync();
}
