using Microsoft.AspNetCore.Mvc;
using Bookstore.Api.Models;

namespace Bookstore.Api.Controllers;

[Route("api/manual-books")]
public class ManualBooksController : ControllerBase
{
    [HttpPost]
    public IActionResult Create([FromBody] CreateBookRequest request)
    {
        // No automatic 400, the check is ours
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        return Ok(request);
    }
}
