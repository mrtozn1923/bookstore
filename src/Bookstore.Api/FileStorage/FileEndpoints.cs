using System.Security.Cryptography;
using Bookstore.Api.Catalog;
using Bookstore.Api.Data;
using Bookstore.Notifications;
using Bookstore.Notifications.Documents;
using Bookstore.Notifications.Documents.Pdf;
using Bookstore.Notifications.Images;
using Bookstore.Notifications.Samples;
using Bookstore.Notifications.Storage;
using Hangfire;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.FileStorage;

public static class FileEndpoints
{
    private static readonly TimeSpan DownloadLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan UploadLifetime = TimeSpan.FromMinutes(10);

    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder app)
    {
        // Ownership is checked here, once; the storage URL itself carries no identity
        app.MapGet("/api/orders/{orderId:int}/invoice/link", async (int orderId, InvoiceArchive archive, IFileStore files, BookstoreContext db,
            TimeProvider time, CancellationToken ct) =>
        {
            InvoiceModel invoice = BookstoreSampleData.Invoice(orderId);
            string path = await archive.EnsureStoredAsync(invoice, ct);
            if (await db.StoredFiles.FindAsync([path], ct) is null)
            {
                db.StoredFiles.Add(await DescribeAsync(files, path, "invoice", FileFormat.Pdf.ContentType, invoice.CustomerEmail, time, ct));
                await db.SaveChangesAsync(ct);
            }

            Uri url = await files.GetDownloadUrlAsync(path, DownloadLifetime, $"invoice-{invoice.InvoiceNumber}.pdf", ct);
            return Results.Ok(new { url, expiresAt = time.GetUtcNow().Add(DownloadLifetime) });
        });

        // Local disk only: the API checks the HMAC signature and streams the file
        app.MapGet("/files/{**path}", async (string path, long expires, string name, string sig, IServiceProvider services, CancellationToken ct) =>
        {
            if (services.GetService<LocalFileStore>() is not { } local || !local.VerifyDownload(path, expires, name, sig))
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            Stream content = await local.OpenReadAsync(path, ct);
            return Results.File(content, "application/octet-stream", name);
        });

        // Step 1: the API hands out a short-lived URL; the image never passes through the API
        app.MapPost("/api/books/{isbn}/cover/uploads", async (string isbn, string contentType, IServiceProvider services, CancellationToken ct) =>
        {
            if (!CoverEndpoints.IsIsbn13(isbn) || contentType is not ("image/jpeg" or "image/png" or "image/webp"))
            {
                return Results.Problem("Not an ISBN-13 or not an accepted image type.", statusCode: StatusCodes.Status400BadRequest);
            }

            if (services.GetService<IDirectUploadStore>() is not { } uploads)
            {
                return Results.Problem("Direct uploads need an object store.", statusCode: StatusCodes.Status501NotImplemented);
            }

            string uploadId = Guid.CreateVersion7().ToString("N");
            DirectUpload upload = await uploads.CreateUploadAsync($"uploads/covers/{isbn}/{uploadId}", contentType, UploadLifetime, ct);
            return Results.Ok(new { uploadId, upload.Url, upload.Method, upload.Headers, upload.ExpiresAt });
        });

        // Step 2: the client says it is done; only now do we look at what arrived
        app.MapPost("/api/books/{isbn}/cover/uploads/{uploadId}/complete", async (string isbn, string uploadId, IFileStore files,
            CoverImageProcessor images, IOptions<CoverImageOptions> options, IBackgroundJobClient jobs, BookstoreContext db, TimeProvider time,
            ILoggerFactory loggers, CancellationToken ct) =>
        {
            if (!CoverEndpoints.IsIsbn13(isbn) || !Guid.TryParseExact(uploadId, "N", out _))
            {
                return Results.NotFound();
            }

            string staging = $"uploads/covers/{isbn}/{uploadId}";
            byte[]? upload;
            try
            {
                await using Stream stream = await files.OpenReadAsync(staging, ct);
                upload = await CoverEndpoints.ReadAtMostAsync(stream, options.Value.MaxBytes, ct);
            }
            catch (FileNotFoundException)
            {
                return Results.NotFound();
            }

            ImageCheck check;
            try
            {
                // A SAS or presigned PUT cannot limit the size or the content: check both here
                check = upload is null
                    ? throw new InvalidImageException($"The upload is larger than {options.Value.MaxBytes:N0} bytes.")
                    : images.Inspect(upload);
            }
            catch (InvalidImageException ex)
            {
                await files.DeleteAsync(staging, ct);
                loggers.CreateLogger("Uploads").LogWarning("Rejected cover upload {UploadId} for {Isbn}: {Reason}", uploadId, isbn, ex.Message);
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status415UnsupportedMediaType);
            }

            (string contentType, string extension) = CoverEndpoints.TypeOf(check.Format);
            string originalPath = $"covers/{isbn}/original{extension}";
            await files.SaveAsync(originalPath, new MemoryStream(upload!), contentType, ct);
            await files.DeleteAsync(staging, ct);
            db.StoredFiles.Add(await DescribeAsync(files, originalPath, "cover-original", contentType, null, time, ct));
            await db.SaveChangesAsync(ct);
            jobs.Enqueue<CoverThumbnailJob>(j => j.CreateAsync(isbn, originalPath, CancellationToken.None));
            return Results.Accepted($"/api/books/{isbn}/cover/320", new { check.Format, check.Width, check.Height, upload!.Length });
        });

        return app;
    }

    private static async Task<StoredFileRecord> DescribeAsync(IFileStore files, string path, string purpose, string contentType, string? ownerId,
        TimeProvider time, CancellationToken ct)
    {
        await using Stream content = await files.OpenReadAsync(path, ct);
        using IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] buffer = new byte[81920];
        long length = 0;
        int read;
        while ((read = await content.ReadAsync(buffer, ct)) > 0)
        {
            sha.AppendData(buffer, 0, read);
            length += read;
        }

        return new StoredFileRecord
        {
            Path = path,
            Purpose = purpose,
            ContentType = contentType,
            Length = length,
            Sha256 = Convert.ToHexStringLower(sha.GetHashAndReset()),
            OwnerId = ownerId,
            CreatedAtUtc = time.GetUtcNow().UtcDateTime,
        };
    }
}
