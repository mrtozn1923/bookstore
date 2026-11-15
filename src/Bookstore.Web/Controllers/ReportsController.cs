using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Web.Controllers;

[Authorize(Roles = "StoreStaff")]
[Route("reports")]
public sealed class ReportsController : Controller
{
    [HttpGet("sales")]
    public IActionResult Sales() => View();
}
