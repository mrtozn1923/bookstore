using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Web.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index() => View();
}
