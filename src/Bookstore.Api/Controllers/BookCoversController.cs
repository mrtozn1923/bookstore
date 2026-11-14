using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace Bookstore.Api.Controllers;

[ApiController]
[Route("api/books/{bookId:int}/cover")]
public class BookCoversController : ControllerBase
{
    private const long MaxFileSize = 2 * 1024 * 1024;

    // allowed extensions and the signature bytes the file must start with
    private static readonly Dictionary<string, byte[]> Signatures = new()
    {
        [".png"] = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A },
        [".jpg"] = new byte[] { 0xFF, 0xD8, 0xFF },
        [".jpeg"] = new byte[] { 0xFF, 0xD8, 0xFF }
    };

    private readonly string _storagePath;

    public BookCoversController(IWebHostEnvironment environment)
    {
        _storagePath = Path.Combine(environment.ContentRootPath, "uploads");
        Directory.CreateDirectory(_storagePath);
    }

    [HttpPost]
    [RequestSizeLimit(3 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 3 * 1024 * 1024)]
    public async Task<IActionResult> Upload(int bookId, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0 || file.Length > MaxFileSize)
        {
            return BadRequest("File size must be between 1 byte and 2 MB.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Signatures.TryGetValue(extension, out var signature))
        {
            return BadRequest("Only .png, .jpg and .jpeg files are allowed.");
        }

        if (!await HasSignatureAsync(file, signature, cancellationToken))
        {
            return BadRequest("File content does not match its extension.");
        }

        // if the book already has a cover (maybe with another extension), delete it first
        var existing = FindCover(bookId);
        if (existing is not null)
        {
            System.IO.File.Delete(existing);
        }

        // the server, not the client, creates the file name
        var fileName = $"cover-{bookId}{extension}";
        var path = Path.Combine(_storagePath, fileName);

        await using (var stream = System.IO.File.Create(path))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return CreatedAtAction(nameof(GetCover), new { bookId }, new { fileName, file.Length });
    }

    [HttpGet]
    public IActionResult GetCover(int bookId)
    {
        var path = FindCover(bookId);
        if (path is null)
        {
            return NotFound();
        }

        // find the MIME type from the extension: .png -> image/png
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(path, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return PhysicalFile(path, contentType, enableRangeProcessing: true);
    }

    [HttpGet("download")]
    public IActionResult DownloadCover(int bookId)
    {
        var path = FindCover(bookId);
        if (path is null)
        {
            return NotFound();
        }

        // FileStreamResult closes the stream, so no using here
        var stream = System.IO.File.OpenRead(path);
        return File(stream, "application/octet-stream", Path.GetFileName(path));
    }

    private string? FindCover(int bookId) =>
        Directory.EnumerateFiles(_storagePath, $"cover-{bookId}.*").FirstOrDefault();

    private static async Task<bool> HasSignatureAsync(IFormFile file, byte[] signature, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var header = new byte[signature.Length];
        var read = await stream.ReadAsync(header, cancellationToken);
        return read == signature.Length && header.SequenceEqual(signature);
    }
}
