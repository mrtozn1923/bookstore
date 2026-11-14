using System.Text.RegularExpressions;

namespace Bookstore.Api.Images;

public static partial class CoverImageEndpoints
{
    public static IEndpointRouteBuilder MapCoverImageEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /img/covers/dune?w=480 -> the smallest allowed width >= w, in the best format the browser accepts
        app.MapGet("/img/covers/{name}", async Task<IResult> (string name, int? w, HttpContext http, CoverImageService images, CancellationToken ct) =>
        {
            if (!SafeName().IsMatch(name))
            {
                return TypedResults.NotFound();
            }
            int width = CoverImageService.Widths.FirstOrDefault(x => x >= (w ?? 1200), CoverImageService.Widths[^1]);
            string accept = http.Request.Headers.Accept.ToString();
            string format = accept.Contains("image/avif") ? "avif" : accept.Contains("image/webp") ? "webp" : "jpeg";

            EncodedImage? image = await images.GetAsync(name, width, format, ct);
            if (image is null)
            {
                return TypedResults.NotFound();
            }
            // Same URL, different bytes per Accept header: caches must key on it
            http.Response.Headers.Vary = "Accept";
            http.Response.Headers.CacheControl = "public, max-age=86400";
            return TypedResults.Bytes(image.Bytes, image.ContentType);
        });
        return app;
    }

    [GeneratedRegex("^[a-z0-9-]{1,60}$")]
    private static partial Regex SafeName();
}
