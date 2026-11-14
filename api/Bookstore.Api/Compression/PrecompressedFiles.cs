using System.Text.RegularExpressions;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

namespace Bookstore.Api.Compression;

// Serves main-ABC123.js.br (written by bookstore-web/tools/precompress.mjs at build time)
// instead of compressing the same file again on every request
public static partial class PrecompressedFiles
{
    // Brotli 11 was smaller than zstd 19 for this build; a ("zstd", ".zst") entry would go here too
    private static readonly (string Encoding, string Extension)[] Encodings = [("br", ".br")];
    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    // Before UseStaticFiles: picks the precompressed copy the browser accepts
    public static IApplicationBuilder UsePrecompressedFiles(this IApplicationBuilder app, IFileProvider files) =>
        app.Use(async (context, next) =>
        {
            string path = context.Request.Path.Value ?? "";
            string acceptEncoding = context.Request.Headers.AcceptEncoding.ToString();
            if (HttpMethods.IsGet(context.Request.Method) && ContentTypes.TryGetContentType(path, out _))
            {
                foreach ((string encoding, string extension) in Encodings)
                {
                    // Simplified Accept-Encoding check (no q-values), enough for browsers
                    if (acceptEncoding.Contains(encoding, StringComparison.OrdinalIgnoreCase) && files.GetFileInfo(path + extension).Exists)
                    {
                        context.Request.Path = path + extension;
                        break;
                    }
                }
            }
            await next();
        });

    public static StaticFileOptions StaticFileOptions()
    {
        FileExtensionContentTypeProvider contentTypes = new();
        contentTypes.Mappings[".br"] = "application/octet-stream";
        return new StaticFileOptions
        {
            ContentTypeProvider = contentTypes,
            // Runs before the body is written, so response compression sees Content-Encoding and skips the file
            OnPrepareResponse = context =>
            {
                string name = context.File.Name;
                IHeaderDictionary headers = context.Context.Response.Headers;
                foreach ((string encoding, string extension) in Encodings)
                {
                    if (name.EndsWith(extension, StringComparison.Ordinal))
                    {
                        name = name[..^extension.Length];
                        headers.ContentEncoding = encoding;
                        headers.Vary = HeaderNames.AcceptEncoding;
                        context.Context.Response.ContentType = ContentTypes.TryGetContentType(name, out string? type) ? type : "application/octet-stream";
                    }
                }
                // main-PL3PRF3A.js: the name changes with the content, so the file never needs revalidation
                headers.CacheControl = HashedName().IsMatch(name)
                    ? "public, max-age=31536000, immutable"
                    : name.EndsWith(".html", StringComparison.Ordinal) ? "no-cache" : headers.CacheControl;
            },
        };
    }

    [GeneratedRegex(@"-[A-Za-z0-9_-]{8}\.(js|css)$")]
    private static partial Regex HashedName();
}
