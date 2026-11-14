using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    [HttpPatch("{id:int}")]
    public ActionResult<Book> Patch(int id, [FromBody] JsonPatchDocument<Book> patch)
    {
        var book = new Book { Id = id, Title = "Dune", Price = 450m, Stock = 10 };

        patch.ApplyTo(book, error => ModelState.AddModelError(error.Operation.path, error.ErrorMessage));
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        return book;
    }
}
