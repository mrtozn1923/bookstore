using Microsoft.AspNetCore.Authorization;
using Bookstore.Api.Models;

namespace Bookstore.Api.Auth;

public class OrderOwnerRequirement : IAuthorizationRequirement
{
}

public class OrderOwnerHandler : AuthorizationHandler<OrderOwnerRequirement, Order>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrderOwnerRequirement requirement,
        Order resource)
    {
        // an admin may cancel any order, a customer only their own
        var userId = context.User.FindFirst("sub")?.Value;
        if (context.User.IsInRole("Admin") || userId == resource.CustomerId.ToString())
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
