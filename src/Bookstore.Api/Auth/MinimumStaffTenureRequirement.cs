using System.Globalization;
using Microsoft.AspNetCore.Authorization;

namespace Bookstore.Api.Auth;

public class MinimumStaffTenureRequirement : IAuthorizationRequirement
{
    public MinimumStaffTenureRequirement(int days)
    {
        Days = days;
    }

    public int Days { get; }
}

public class MinimumStaffTenureHandler : AuthorizationHandler<MinimumStaffTenureRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MinimumStaffTenureRequirement requirement)
    {
        var claim = context.User.FindFirst("staff_since");
        if (claim is not null &&
            DateTime.TryParseExact(claim.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var since) &&
            since.AddDays(requirement.Days) <= DateTime.UtcNow)
        {
            context.Succeed(requirement);
        }

        // if Succeed is not called, the requirement counts as not met
        return Task.CompletedTask;
    }
}
