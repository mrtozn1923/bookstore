using System.Security.Cryptography;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Bookstore.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Storage;

public sealed record CoverUploaded(string Isbn, string BlobName, string ContentType, long Size);

public static class CoverEndpoints
{
    public static IEndpointRouteBuilder MapCoverEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder covers = app.MapGroup("/api/books/{isbn}/cover").WithTags("Covers");
        // Store staff only once authentication is added; the form is read by an API client, not a browser page
        covers.MapPut("/", UploadCover)
            .DisableAntiforgery()
            .WithFormOptions(multipartBodyLengthLimit: CoverImage.MaxBytes + 64 * 1024);
        covers.MapGet("/", RedirectToCover);
        return app;
    }

    private static async Task<Results<Created<CoverUploaded>, NotFound, ProblemHttpResult>> UploadCover(
        string isbn, IFormFile file, BookstoreContext db, BlobContainerClient covers,
        ILoggerFactory loggerFactory, CancellationToken ct)
    {
        var book = await db.Books.Where(b => b.Isbn == isbn).Select(b => new { b.Id, b.CoverBlobName }).FirstOrDefaultAsync(ct);
        if (book is null)
        {
            return TypedResults.NotFound();
        }
        if (file.Length is 0 or > CoverImage.MaxBytes)
        {
            return TypedResults.Problem($"The cover must be between 1 byte and {CoverImage.MaxBytes} bytes.", statusCode: 400);
        }

        await using Stream stream = file.OpenReadStream();
        byte[] header = new byte[12];
        int read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, ct);
        if (CoverImage.Detect(header.AsSpan(0, read)) is not { } format)
        {
            return TypedResults.Problem("Only JPEG, PNG and WebP images are accepted.", statusCode: 415);
        }
        stream.Position = 0;

        // Never the client's file name: a random name can't be guessed, can't collide and needs no escaping
        string blobName = $"{isbn}-{RandomNumberGenerator.GetHexString(32, lowercase: true)}{format.Extension}";
        BlobClient blob = covers.GetBlobClient(blobName);
        await blob.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = format.ContentType, CacheControl = "public, max-age=31536000, immutable" },
            Metadata = new Dictionary<string, string> { ["isbn"] = isbn },
            Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All },
        }, ct);

        await db.Books.Where(b => b.Id == book.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.CoverBlobName, blobName), ct);

        if (book.CoverBlobName is { } oldName)
        {
            // Soft delete keeps the old cover for the retention period
            await covers.DeleteBlobIfExistsAsync(oldName, cancellationToken: ct);
        }

        loggerFactory.CreateLogger(typeof(CoverEndpoints)).LogInformation(
            "Stored cover {BlobName} ({Size} bytes, {ContentType}) for {Isbn}", blobName, file.Length, format.ContentType, isbn);
        return TypedResults.Created($"/api/books/{isbn}/cover", new CoverUploaded(isbn, blobName, format.ContentType, file.Length));
    }

    private static async Task<Results<RedirectHttpResult, NotFound>> RedirectToCover(
        string isbn, BookstoreContext db, CoverUrlSigner signer, HttpContext http, TimeProvider clock, CancellationToken ct)
    {
        string? blobName = await db.Books.Where(b => b.Isbn == isbn).Select(b => b.CoverBlobName).FirstOrDefaultAsync(ct);
        if (blobName is null)
        {
            return TypedResults.NotFound();
        }

        SignedCoverUrl signed = await signer.SignAsync(blobName, ct);
        // Cache the redirect at most until the signed URL expires (minus a margin)
        int maxAge = Math.Max(0, (int)(signed.ExpiresOn - clock.GetUtcNow()).TotalSeconds - 300);
        http.Response.Headers.CacheControl = $"private, max-age={maxAge}";
        return TypedResults.Redirect(signed.Url.ToString());
    }
}
