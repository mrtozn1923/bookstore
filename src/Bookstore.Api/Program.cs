using Bookstore.Institutional.Application;
using Bookstore.Institutional.Infrastructure;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
string connectionString = builder.Configuration.GetConnectionString("Bookstore")
    ?? "Server=localhost;Database=BookstoreDb;Trusted_Connection=True;TrustServerCertificate=True";
builder.Services.AddInstitutionalOrdering(connectionString);
WebApplication app = builder.Build();

if (args.Contains("--script"))
{
    using IServiceScope scope = app.Services.CreateScope();
    Console.WriteLine(scope.ServiceProvider.GetRequiredService<BookstoreContext>().Database.GenerateCreateScript());
    return;
}

RouteGroupBuilder orders = app.MapGroup("/orders");
orders.MapPost("/", (CreateOrderCommand command, IOrderService service, CancellationToken ct) =>
    service.CreateAsync(command, ct));
orders.MapPost("/{id:int}/submit", (int id, IOrderService service, CancellationToken ct) =>
    service.SubmitAsync(id, ct));
orders.MapPost("/{id:int}/approve", (int id, DecisionBody body, IOrderService service, CancellationToken ct) =>
    service.ApproveAsync(new DecideOrderCommand(id, body.ApproverId, body.Comment), ct));
orders.MapPost("/{id:int}/reject", (int id, DecisionBody body, IOrderService service, CancellationToken ct) =>
    service.RejectAsync(new DecideOrderCommand(id, body.ApproverId, body.Comment), ct));
orders.MapPost("/{id:int}/cancel", (int id, IOrderService service, CancellationToken ct) =>
    service.CancelAsync(id, ct));

app.Run();

public sealed record DecisionBody(int ApproverId, string? Comment);
