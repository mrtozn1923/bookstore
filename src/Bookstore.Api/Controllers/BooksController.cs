using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    [HttpGet]
    public IEnumerable<string> GetAll() => new[] { "Dune", "Emma", "The Hobbit" };
}
