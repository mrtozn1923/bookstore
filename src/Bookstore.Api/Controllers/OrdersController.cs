using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Bookstore.Api.Models;

namespace Bookstore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
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
}
