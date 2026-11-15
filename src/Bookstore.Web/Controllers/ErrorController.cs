using System.Diagnostics;
using Bookstore.Web.Catalog;
using Bookstore.Web.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Web.Controllers;

[AllowAnonymous]
[Route("error")]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class ErrorController(IBookCatalog catalog, ILogger<ErrorController> logger) : Controller
{
    // No [HttpGet]: an exception thrown during a POST is re-executed as a POST
    [Route("")]
    public IActionResult ServerError()
    {
        IExceptionHandlerFeature? feature = HttpContext.Features.Get<IExceptionHandlerFeature>();
        if (feature is null)
        {
            return NotFound(); // someone typed /error into the address bar
        }

        string errorCode = Activity.Current?.TraceId.ToString() ?? HttpContext.TraceIdentifier;
        string? userMessage = (feature.Error as DomainException)?.UserMessage;
        return View(new ServerErrorViewModel(Response.StatusCode, errorCode, userMessage));
    }

    [Route("{statusCode:int}")]
    public async Task<IActionResult> Status(int statusCode, CancellationToken cancellationToken)
    {
        // Direct visits to /error/404 must not answer 200
        Response.StatusCode = statusCode;
        IStatusCodeReExecuteFeature? reExecute = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();

        if (statusCode == StatusCodes.Status404NotFound)
        {
            IReadOnlyList<Book> suggestions = [];
            try
            {
                suggestions = await catalog.SuggestAsync(reExecute?.OriginalPath, 3, cancellationToken);
            }
            catch (Exception ex)
            {
                // The 404 page must work even when the catalog does not
                logger.LogWarning(ex, "No suggestions for the 404 page");
            }

            return View("NotFound", new NotFoundViewModel(reExecute?.OriginalPath, suggestions));
        }

        return statusCode == StatusCodes.Status403Forbidden ? View("Forbidden") : View("Status", statusCode);
    }
}
