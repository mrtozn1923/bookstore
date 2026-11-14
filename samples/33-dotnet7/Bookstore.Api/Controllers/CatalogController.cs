using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Controllers;

[ApiController]
[Route("api/catalog")]
public class CatalogController : ControllerBase
{
    // No [FromServices]
    [HttpGet]
    public ActionResult<IEnumerable<Book>> GetAll(IBookService service)
    {
        return Ok(service.GetAll());
    }
}
