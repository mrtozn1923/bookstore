using Microsoft.AspNetCore.Mvc;
using Bookstore.Api.Services;

namespace Bookstore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotifiersController : ControllerBase
{
    private readonly INotifier _notifier;
    private readonly IEnumerable<INotifier> _all;

    public NotifiersController(INotifier notifier, IEnumerable<INotifier> all)
    {
        _notifier = notifier;
        _all = all;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { single = _notifier.Name, all = _all.Select(n => n.Name) });
    }
}
