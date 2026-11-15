using System.Diagnostics;
using System.Text.RegularExpressions;
using Hangfire;
using Bookstore.Notifications;
using Bookstore.Notifications.Images;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Catalog;

// A Hangfire job since post 09: decoding and resizing is CPU work, done outside the upload request
public sealed class CoverThumbnailJob(CoverImageProcessor images, IFileStore files, ILogger<CoverThumbnailJob> logger)
{
    public async Task CreateAsync(string isbn, string originalPath, CancellationToken cancellationToken)
    {
        Stopwatch watch = Stopwatch.StartNew();
        using MemoryStream original = new();
        await using (Stream stored = await files.OpenReadAsync(originalPath, cancellationToken))
        {
            await stored.CopyToAsync(original, cancellationToken);
        }

        // Same widths, same paths: running the job twice overwrites the thumbnails with identical files
        foreach (CoverThumbnail thumbnail in images.CreateThumbnails(original.GetBuffer().AsSpan(0, (int)original.Length)))
        {
            await files.SaveAsync(CoverEndpoints.ThumbnailPath(isbn, thumbnail.Width), new MemoryStream(thumbnail.Content), thumbnail.ContentType, cancellationToken);
        }

        logger.LogInformation("Cover thumbnails for {Isbn} ready in {Elapsed} ms", isbn, watch.ElapsedMilliseconds);
    }
}

public static partial class CoverEndpoints
{
    [GeneratedRegex("^97[89][0-9]{10}$")]
    private static partial Regex Isbn13();

    public static string ThumbnailPath(string isbn, int width) => $"covers/{isbn}/{width}.webp";

    public static bool IsIsbn13(string value) => Isbn13().IsMatch(value);

    public static (string ContentType, string Extension) TypeOf(UploadedImageFormat format) => format switch
    {
        UploadedImageFormat.Jpeg => ("image/jpeg", ".jpg"),
        UploadedImageFormat.Png => ("image/png", ".png"),
        _ => ("image/webp", ".webp"),
    };

    public static IEndpointRouteBuilder MapCoverEndpoints(this IEndpointRouteBuilder app)
    {
        // The raw image is the request body; the HTTP side of uploads (IFormFile, limits) is covered in the Web API series
        app.MapPost("/api/books/{isbn}/cover", async (string isbn, HttpRequest request, CoverImageProcessor images, IFileStore files,
            IBackgroundJobClient jobs, IOptions<CoverImageOptions> options, CancellationToken ct) =>
        {
            if (!Isbn13().IsMatch(isbn))
            {
                return Results.Problem("Not an ISBN-13.", statusCode: StatusCodes.Status400BadRequest);
            }

            byte[]? upload = await ReadAtMostAsync(request.Body, options.Value.MaxBytes, ct);
            if (upload is null)
            {
                return Results.Problem($"The image is larger than {options.Value.MaxBytes:N0} bytes.", statusCode: StatusCodes.Status413PayloadTooLarge);
            }

            ImageCheck check;
            try
            {
                check = images.Inspect(upload);
            }
            catch (InvalidImageException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status415UnsupportedMediaType);
            }

            (string contentType, string extension) = TypeOf(check.Format);

            // The original stays private: it may carry EXIF data such as the location where the photo was taken
            string originalPath = $"covers/{isbn}/original{extension}";
            await files.SaveAsync(originalPath, new MemoryStream(upload), contentType, ct);
            jobs.Enqueue<CoverThumbnailJob>(j => j.CreateAsync(isbn, originalPath, CancellationToken.None));
            return Results.Accepted($"/api/books/{isbn}/cover/320", new { check.Format, check.Width, check.Height, check.Origin });
        });

        app.MapGet("/api/books/{isbn}/cover/{width:int}", async (string isbn, int width, IFileStore files, CancellationToken ct) =>
        {
            if (!Isbn13().IsMatch(isbn) || !await files.ExistsAsync(ThumbnailPath(isbn, width), ct))
            {
                return Results.NotFound();
            }

            return Results.Stream(await files.OpenReadAsync(ThumbnailPath(isbn, width), ct), "image/webp");
        });

        return app;
    }

    public static async Task<byte[]?> ReadAtMostAsync(Stream body, int maxBytes, CancellationToken ct)
    {
        using MemoryStream buffer = new();
        byte[] chunk = new byte[81920];
        int read;
        while ((read = await body.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > maxBytes)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }
}
