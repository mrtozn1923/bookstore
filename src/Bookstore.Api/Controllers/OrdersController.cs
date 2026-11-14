using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bookstore.Api.Models;
using Bookstore.Api.Services;

namespace Bookstore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderStore _store;
    private readonly IAuthorizationService _authorizationService;

    public OrdersController(OrderStore store, IAuthorizationService authorizationService)
    {
        _store = store;
        _authorizationService = authorizationService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateOrderRequest request,
        [FromServices] IValidator<CreateOrderRequest> validator)
    {
        ValidationResult result = await validator.ValidateAsync(request);
        if (!result.IsValid)
        {
            foreach (ValidationFailure error in result.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        return Ok(new { message = "Order accepted", itemCount = request.Items.Count });
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Policy = "CanCancelOrders")]
    public async Task<IActionResult> Cancel(int id)
    {
        var order = _store.Find(id);
        if (order is null)
        {
            return NotFound();
        }

        // we decide only after we have seen the order
        var result = await _authorizationService.AuthorizeAsync(User, order, "OrderOwner");
        if (!result.Succeeded)
        {
            return Forbid();
        }

        order.Status = "Cancelled";
        return NoContent();
    }
}
