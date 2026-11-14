using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Controllers;

// Controller: the key goes on the constructor parameter
[ApiController]
[Route("api/orders")]
public class OrdersController([FromKeyedServices("email")] INotificationService notifier) : ControllerBase
{
    [HttpPost("{id:int}/confirm")]
    public IActionResult Confirm(int id)
    {
        return Ok(notifier.Send(id));
    }
}
