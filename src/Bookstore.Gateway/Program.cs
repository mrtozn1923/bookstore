using Yarp.ReverseProxy.Model;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapGet("/gateway/ping", () => "pong");

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
            "Forwarded to {DestinationId}, status {StatusCode}",
            proxy.ProxiedDestination?.DestinationId,
            context.Response.StatusCode);
    });
    proxyPipeline.UseSessionAffinity();
    proxyPipeline.UseLoadBalancing();
    proxyPipeline.UsePassiveHealthChecks();
});

app.Run();
