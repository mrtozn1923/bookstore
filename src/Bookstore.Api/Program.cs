using Bookstore.Api.Demo;
using Bookstore.Institutional.Application;
using Bookstore.Institutional.Infrastructure;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
string connectionString = builder.Configuration.GetConnectionString("Bookstore")
    ?? "Server=localhost;Database=BookstoreDb;Trusted_Connection=True;TrustServerCertificate=True";
builder.Services.AddInstitutionalOrdering(connectionString);
if (args.Contains("--demo"))
    builder.Services.AddDemoServices();
WebApplication app = builder.Build();

if (args.Contains("--script"))
{
    using IServiceScope scope = app.Services.CreateScope();
    Console.WriteLine(scope.ServiceProvider.GetRequiredService<BookstoreContext>().Database.GenerateCreateScript());
    return;
}

if (args.Contains("--demo"))
    await DemoData.ResetAsync(app.Services);

app.MapPost("/orders", async (CreateOrderCommand command, IOrderService service, CancellationToken ct) =>
{
    try
    {
        OrderResult result = await service.CreateAsync(command, ct);
        return Results.Created($"/orders/{result.Id}", result);
    }
    catch (OrderRuleException ex)
    {
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status409Conflict);
    }
});

RouteGroupBuilder orders = app.MapGroup("/orders/{id:int}");
orders.MapPost("/submit", (int id, IOrderService service, CancellationToken ct) =>
    Decide(() => service.SubmitAsync(id, ct)));
orders.MapPost("/approve", (int id, DecisionBody body, IOrderService service, ILogger<Program> log,
    CancellationToken ct) => Decide(async () =>
    {
        OrderResult result = await service.ApproveAsync(new DecideOrderCommand(id, body.ApproverId, body.Comment), ct);
        log.LogInformation("User {ApproverId} approved #{Id} -> {Status}", body.ApproverId, id, result.Status);
        return result;
    }));
orders.MapPost("/reject", (int id, DecisionBody body, IOrderService service, CancellationToken ct) =>
    Decide(() => service.RejectAsync(new DecideOrderCommand(id, body.ApproverId, body.Comment), ct)));
orders.MapPost("/cancel", (int id, IOrderService service, CancellationToken ct) =>
    Decide(() => service.CancelAsync(id, ct)));

app.Run();

// Kural ihlali 409, diğer her şey 200
static async Task<IResult> Decide(Func<Task<OrderResult>> action)
{
    try
    {
        return Results.Ok(await action());
    }
    catch (OrderRuleException ex)
    {
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status409Conflict);
    }
}

public sealed record DecisionBody(int ApproverId, string? Comment);
