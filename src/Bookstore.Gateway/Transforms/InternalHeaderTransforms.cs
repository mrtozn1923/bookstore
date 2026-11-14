using System.Security.Claims;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Bookstore.Gateway.Transforms;

public static class InternalHeaderTransforms
{
    // Runs once per route when the config is (re)loaded, not once per request.
    public static void Apply(TransformBuilderContext context)
    {
        string routeId = context.Route.RouteId;

        context.AddRequestTransform(transformContext =>
        {
            foreach (string name in transformContext.HttpContext.Request.Headers.Keys)
            {
                if (name.StartsWith(InternalHeaders.Prefix, StringComparison.OrdinalIgnoreCase))
                {
                    transformContext.ProxyRequest.Headers.Remove(name);
                }
            }

            transformContext.ProxyRequest.Headers.TryAddWithoutValidation(InternalHeaders.Route, routeId);

            // Only after authentication: the value comes from a validated token, never from the client.
            string? userId = transformContext.HttpContext.User.FindFirstValue("sub");
            if (userId is not null)
            {
                transformContext.ProxyRequest.Headers.TryAddWithoutValidation(InternalHeaders.UserId, userId);
            }

            return ValueTask.CompletedTask;
        });
    }
}
