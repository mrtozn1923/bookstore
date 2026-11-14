using Bookstore.Gateway.Configuration;
using Bookstore.Gateway.LoadBalancing;
using Bookstore.Gateway.Security;
using Bookstore.Gateway.Traffic;
using Bookstore.Gateway.Transforms;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.LoadBalancing;
using Yarp.ReverseProxy.Model;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGatewayAuthentication(builder.Configuration);
builder.Services.AddGatewayTrafficPolicies();
builder.Services.AddServiceDiscovery();
builder.Services.AddSingleton<ILoadBalancingPolicy, PreferSameZonePolicy>();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(InternalHeaderTransforms.Apply)
    .AddConfigFilter<DestinationAddressFilter>()
    .AddServiceDiscoveryDestinationResolver();

string? gatewayDb = builder.Configuration.GetConnectionString("GatewayConfig");
if (!string.IsNullOrEmpty(gatewayDb))
{
    builder.Services.AddSingleton(sp => new SqlProxyConfigProvider(
        gatewayDb,
        TimeSpan.FromSeconds(10),
        sp.GetRequiredService<ILogger<SqlProxyConfigProvider>>()));
    builder.Services.AddSingleton<IProxyConfigProvider>(sp => sp.GetRequiredService<SqlProxyConfigProvider>());
    builder.Services.AddHostedService(sp => sp.GetRequiredService<SqlProxyConfigProvider>());
}

var app = builder.Build();

// Order matters: CORS answers preflights, authentication sets User for the rate limiter partition,
// authorization rejects before anything is cached or forwarded.
app.UseCors();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.UseRequestTimeouts();
app.UseOutputCache();

app.MapGet("/gateway/ping", () => "pong").AllowAnonymous();

app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.Use(async (context, next) =>
    {
        IReverseProxyFeature proxy = context.GetReverseProxyFeature();
        ILogger logger = context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Bookstore.Gateway");

        logger.LogInformation(
            "Route {RouteId} -> cluster {ClusterId}, {Count} available destination(s)",
            proxy.Route.Config.RouteId,
            proxy.Cluster.Config.ClusterId,
            proxy.AvailableDestinations.Count);

        await next();

        logger.LogInformation(
            "Forwarded to {DestinationId}, status {StatusCode}, error {Error}",
            proxy.ProxiedDestination?.DestinationId,
            context.Response.StatusCode,
            context.GetForwarderErrorFeature()?.Error.ToString() ?? "none");
    });
    proxyPipeline.UseRequestTimeoutStatus();
    proxyPipeline.UseSessionAffinity();
    proxyPipeline.UseLoadBalancing();
    proxyPipeline.UsePassiveHealthChecks();
});

app.Run();
