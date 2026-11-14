using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Bookstore.Api.Filters;

namespace Bookstore.Api.Controllers;

[ApiController]
[Route("api/books/{bookId:int}/ebook")]
public class BookEbooksController : ControllerBase
{
    private readonly string _storagePath;

    public BookEbooksController(IWebHostEnvironment environment)
    {
        _storagePath = Path.Combine(environment.ContentRootPath, "uploads");
        Directory.CreateDirectory(_storagePath);
    }

    [HttpPost]
    [RequestSizeLimit(200 * 1024 * 1024)]
    [DisableFormValueModelBinding]
    public async Task<IActionResult> Upload(int bookId, CancellationToken cancellationToken)
    {
        if (!MediaTypeHeaderValue.TryParse(Request.ContentType, out var mediaType) ||
            !mediaType.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Expected a multipart/form-data request.");
        }

        var boundary = HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value;
        if (string.IsNullOrEmpty(boundary))
        {
            return BadRequest("Missing multipart boundary.");
        }

        // we read the body section by section; nothing is buffered in memory up front
        var reader = new MultipartReader(boundary, Request.Body);
        var section = await reader.ReadNextSectionAsync(cancellationToken);

        while (section is not null)
        {
            if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var disposition) &&
                disposition.IsFileDisposition())
            {
                var path = Path.Combine(_storagePath, $"ebook-{bookId}.pdf");
                await using var target = System.IO.File.Create(path);
                await section.Body.CopyToAsync(target, cancellationToken);

                return Ok(new { fileName = Path.GetFileName(path), length = target.Length });
            }

            section = await reader.ReadNextSectionAsync(cancellationToken);
        }

        return BadRequest("No file section found.");
    }
}
