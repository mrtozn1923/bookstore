using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiagnosticsController : ControllerBase
{
    [HttpGet("boom")]
    public IActionResult Boom()
    {
        throw new InvalidOperationException("Stock service is unreachable.");
    }
}
