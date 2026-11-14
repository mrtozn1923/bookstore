using Bookstore.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Catalog;

public sealed record CoverUploaded(string Isbn, string FileName, string ContentType, long Size);

// Book cover images, stored on disk under App_Data/covers (development storage).
public static class BookCovers
{
    private const long MaxSize = 2 * 1024 * 1024;

    private static readonly Dictionary<string, (string Extension, byte[] Signature)> Allowed = new()
    {
        ["image/jpeg"] = (".jpg", [0xFF, 0xD8, 0xFF]),
        ["image/png"] = (".png", [0x89, 0x50, 0x4E, 0x47])
    };

    public static RouteGroupBuilder MapBookCoverEndpoints(this RouteGroupBuilder catalog)
    {
        // Bearer token, no cookies: a cross-site form cannot send the token, so antiforgery is not needed.
        catalog.MapPut("/books/{isbn}/cover", UploadCover)
            .RequireAuthorization(policy => policy.RequireRole("StoreStaff"))
            .DisableAntiforgery();
        catalog.MapGet("/books/{isbn}/cover", GetCover);
        return catalog;
    }

    // PUT /api/books/{isbn}/cover  multipart/form-data, field "file"
    private static async Task<Results<Ok<CoverUploaded>, NotFound, ProblemHttpResult>> UploadCover(
        string isbn, IFormFile file, BookstoreContext db, IWebHostEnvironment env, CancellationToken ct)
    {
        if (!await db.Books.AnyAsync(b => b.Isbn == isbn, ct))
        {
            return TypedResults.NotFound();
        }
        if (file.Length is 0 or > MaxSize)
        {
            return TypedResults.Problem($"The cover must be between 1 byte and {MaxSize} bytes.", statusCode: StatusCodes.Status400BadRequest);
        }
        if (!Allowed.TryGetValue(file.ContentType, out var format) || !await StartsWithAsync(file, format.Signature, ct))
        {
            return TypedResults.Problem("Only JPEG and PNG covers are accepted.", statusCode: StatusCodes.Status415UnsupportedMediaType);
        }

        string folder = Path.Combine(env.ContentRootPath, "App_Data", "covers");
        Directory.CreateDirectory(folder);
        foreach (string old in Directory.EnumerateFiles(folder, isbn + ".*"))
        {
            File.Delete(old);
        }

        string fileName = isbn + format.Extension;
        await using (FileStream target = File.Create(Path.Combine(folder, fileName)))
        {
            await file.CopyToAsync(target, ct);
        }
        return TypedResults.Ok(new CoverUploaded(isbn, fileName, file.ContentType, file.Length));
    }

    private static Results<PhysicalFileHttpResult, NotFound> GetCover(string isbn, IWebHostEnvironment env)
    {
        string folder = Path.Combine(env.ContentRootPath, "App_Data", "covers");
        string? path = Directory.Exists(folder) ? Directory.EnumerateFiles(folder, isbn + ".*").FirstOrDefault() : null;
        return path is null
            ? TypedResults.NotFound()
            : TypedResults.PhysicalFile(path, path.EndsWith(".png") ? "image/png" : "image/jpeg");
    }

    private static async Task<bool> StartsWithAsync(IFormFile file, byte[] signature, CancellationToken ct)
    {
        byte[] head = new byte[signature.Length];
        await using Stream stream = file.OpenReadStream();
        int read = await stream.ReadAtLeastAsync(head, head.Length, throwOnEndOfStream: false, ct);
        return read == head.Length && head.AsSpan().SequenceEqual(signature);
    }
}
