using System.Diagnostics;
using System.Net;
using Bookstore.Gateway.Security;
using Bookstore.Gateway.Transforms;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Bookstore.Gateway.Forwarding;

public static class InstitutionForwarding
{
    public static IEndpointRouteBuilder MapInstitutionForwarding(this IEndpointRouteBuilder app)
    {
        // One invoker for the app's lifetime; HttpMessageInvoker, not HttpClient, so responses are streamed.
        HttpMessageInvoker httpClient = new(new SocketsHttpHandler
        {
            UseProxy = false,
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.None,
            UseCookies = false,
            EnableMultipleHttp2Connections = true,
            ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current),
            ConnectTimeout = TimeSpan.FromSeconds(15),
        });
        ForwarderRequestConfig requestConfig = new() { ActivityTimeout = TimeSpan.FromSeconds(30) };

        // Direct forwarding does not run AddTransforms; the internal header rule is applied here again.
        HttpTransformer transformer = app.ServiceProvider.GetRequiredService<ITransformBuilder>()
            .Create(context => InternalHeaderTransforms.Add(context, "institutions")
                .AddPathRemovePrefix("/api/v1"));

        app.Map("/api/v1/institutions/{institutionId}/{**rest}", async (
            HttpContext context, string institutionId, IHttpForwarder forwarder, IConfiguration configuration) =>
        {
            // Large institutions have a dedicated Ordering instance; everyone else shares one.
            string destination = configuration[$"Institutions:Dedicated:{institutionId}"]
                ?? configuration["Institutions:Shared"]!;

            ForwarderError error = await forwarder.SendAsync(context, destination, httpClient, requestConfig, transformer);
            if (error != ForwarderError.None)
            {
                context.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Bookstore.Gateway.Forwarding")
                    .LogWarning("Institution {InstitutionId} -> {Destination} failed: {Error}", institutionId, destination, error);
            }
        }).RequireAuthorization(GatewayAuthentication.InstitutionBuyerPolicy);

        return app;
    }
}
