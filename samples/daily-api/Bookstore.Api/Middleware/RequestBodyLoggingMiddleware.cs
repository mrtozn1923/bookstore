namespace Bookstore.Api.Middleware;

public class RequestBodyLoggingMiddleware(RequestDelegate next, ILogger<RequestBodyLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method) && context.Request.Path.StartsWithSegments("/api/institution/orders"))
        {
            // Body'yi belleğe/diske alıp tekrar okunabilir yapar
            context.Request.EnableBuffering();

            using StreamReader reader = new(context.Request.Body, leaveOpen: true);
            string body = await reader.ReadToEndAsync(context.RequestAborted);
            logger.LogInformation("Institutional order body ({Length} chars): {Body}", body.Length, body);

            // Sonraki okuyucu baştan başlasın
            context.Request.Body.Position = 0;
        }

        await next(context);
    }
}
