using System.Security.Claims;

namespace Bookstore.Institutional.Workflow;

public sealed record ActionRequest(string? Comment);

public static class WorkflowEndpoints
{
    public static void MapOrderWorkflow(this WebApplication app)
    {
        app.MapGet("/api/orders/{id:int}/actions", async (int id, OrderWorkflow wf, ClaimsPrincipal user) =>
            Results.Ok(await wf.AllowedActionsAsync(id, user.ToUserInfo())));

        app.MapPost("/api/orders/{id:int}/actions/{action}", async (int id, string action, ActionRequest body, OrderWorkflow wf, ClaimsPrincipal user) =>
            (await wf.ExecuteAsync(id, action, user.ToUserInfo(), body.Comment)).ToHttpResult());
    }

    public static IResult ToHttpResult(this TransitionResult result) => result switch
    {
        TransitionResult.Done => Results.NoContent(),
        TransitionResult.NotFound => Results.NotFound(),
        TransitionResult.Forbidden => Results.Forbid(),
        TransitionResult.CommentRequired => Results.ValidationProblem(
            new Dictionary<string, string[]> { ["comment"] = ["A comment is required for this action."] }),
        TransitionResult.InvalidTransition => Results.Problem(statusCode: 409, title: "This action is not allowed in the current status."),
        TransitionResult.Conflict => Results.Problem(statusCode: 409, title: "The order was changed by someone else. Reload and try again."),
        _ => Results.StatusCode(500)
    };

    public static UserInfo ToUserInfo(this ClaimsPrincipal principal)
        => new(principal.Identity?.Name ?? "", principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray());
}
