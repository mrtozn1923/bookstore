namespace Bookstore.Api.Ssr;

// Pages are rendered by the Angular SSR server (Node.js, loopback only); this API stays the single origin
public static class SsrProxy
{
    public static async Task ForwardAsync(HttpContext context, IHttpClientFactory httpClientFactory)
    {
        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            return;
        }

        HttpClient client = httpClientFactory.CreateClient("ssr");
        using HttpRequestMessage request = new(HttpMethod.Get, context.Request.Path + context.Request.QueryString);
        // Angular checks the host against security.allowedHosts (SSRF protection)
        request.Headers.Host = context.Request.Host.Value;
        request.Headers.TryAddWithoutValidation("Accept", context.Request.Headers.Accept.ToString());
        request.Headers.TryAddWithoutValidation("Accept-Language", context.Request.Headers.AcceptLanguage.ToString());
        // No cookies are forwarded: the rendered page is the same for every user and can be cached

        using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
        context.Response.StatusCode = (int)response.StatusCode;
        context.Response.ContentType = response.Content.Headers.ContentType?.ToString();
        if (response.Headers.CacheControl is { } cacheControl)
        {
            context.Response.Headers.CacheControl = cacheControl.ToString();
        }
        await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
    }
}
