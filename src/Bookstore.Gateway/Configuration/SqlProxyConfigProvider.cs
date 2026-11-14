using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace Bookstore.Gateway.Configuration;

// Reads extra routes from dbo.GatewayRoutes. Clusters stay in appsettings.json;
// a route here may point to any cluster defined by another config source.
public sealed class SqlProxyConfigProvider(
    string connectionString,
    TimeSpan pollInterval,
    ILogger<SqlProxyConfigProvider> logger) : BackgroundService, IProxyConfigProvider
{
    private volatile SqlProxyConfig _config = new([], "empty");

    public IProxyConfig GetConfig() => _config;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(pollInterval);
        do
        {
            try
            {
                await ReloadAsync(stoppingToken);
            }
            catch (SqlException ex)
            {
                // Keep serving the last good config when the database is unreachable.
                logger.LogWarning(ex, "Could not read gateway routes, keeping revision {Revision}", _config.RevisionId);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ReloadAsync(CancellationToken cancellationToken)
    {
        List<RouteConfig> routes = [];
        StringBuilder fingerprint = new();

        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using SqlCommand command = new(
            """
            SELECT RouteId, ClusterId, Path, [Order], PathPattern
            FROM dbo.GatewayRoutes
            WHERE IsEnabled = 1
            ORDER BY RouteId
            """, connection);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            RouteConfig route = new()
            {
                RouteId = reader.GetString(0),
                ClusterId = reader.GetString(1),
                Match = new RouteMatch { Path = reader.GetString(2) },
                Order = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                Transforms = reader.IsDBNull(4)
                    ? null
                    : [new Dictionary<string, string> { ["PathPattern"] = reader.GetString(4) }],
            };
            routes.Add(route);
            fingerprint.Append(route.RouteId).Append('|').Append(route.ClusterId).Append('|')
                .Append(route.Match.Path).Append('|').Append(route.Order).Append('|')
                .Append(reader.IsDBNull(4) ? "" : reader.GetString(4)).Append(';');
        }

        string revision = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint.ToString())))[..12];
        if (revision == _config.RevisionId)
        {
            return;
        }

        SqlProxyConfig old = _config;
        _config = new SqlProxyConfig(routes, revision);
        old.SignalChange();
        logger.LogInformation("Loaded {Count} route(s) from SQL, revision {Revision}", routes.Count, revision);
    }

    private sealed class SqlProxyConfig : IProxyConfig
    {
        private readonly CancellationTokenSource _changed = new();

        public SqlProxyConfig(IReadOnlyList<RouteConfig> routes, string revisionId)
        {
            Routes = routes;
            RevisionId = revisionId;
            ChangeToken = new CancellationChangeToken(_changed.Token);
        }

        public string RevisionId { get; }
        public IReadOnlyList<RouteConfig> Routes { get; }
        public IReadOnlyList<ClusterConfig> Clusters { get; } = [];
        public IChangeToken ChangeToken { get; }

        public void SignalChange() => _changed.Cancel();
    }
}
