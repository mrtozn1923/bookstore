using Microsoft.AspNetCore.Http.Timeouts;
using Yarp.ReverseProxy.Forwarder;

namespace Bookstore.Gateway.Traffic;

public static class RequestTimeoutStatus
{
    // When the request timeouts middleware cancels a proxied request, YARP sees a canceled
    // HttpContext.RequestAborted, reports RequestCanceled and answers 400 as if the client left.
    // This step turns that case back into 504 Gateway Timeout.
    public static IReverseProxyApplicationBuilder UseRequestTimeoutStatus(this IReverseProxyApplicationBuilder proxyPipeline)
    {
        proxyPipeline.Use(async (context, next) =>
        {
            await next();

            bool timedOut = context.Features.Get<IHttpRequestTimeoutFeature>()?.RequestTimeoutToken.IsCancellationRequested == true;
            if (timedOut
                && context.GetForwarderErrorFeature()?.Error == ForwarderError.RequestCanceled
                && !context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
            }
        });
        return proxyPipeline;
    }
}
