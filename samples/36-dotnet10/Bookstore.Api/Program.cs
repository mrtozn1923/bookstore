using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Bookstore.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi(options =>
{
    options.AddSchemaTransformer((schema, context, cancellationToken) =>
    {
        if (context.JsonTypeInfo.Type == typeof(decimal))
        {
            schema.Example = JsonValue.Create("450");
        }
        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.MapOpenApi();
app.MapOpenApi("/openapi/{documentName}.yaml");

app.MapPost("/books", (CreateBookRequest request) =>
    TypedResults.Created($"/books/1", new BookDto(1, request.Title, request.Price, request.Description)));

app.MapGet("/orders/{id:int}/status-stream", (int id, CancellationToken cancellationToken) =>
    TypedResults.ServerSentEvents(GetStatusesAsync(id, cancellationToken), eventType: "order-status"));

app.MapControllers();

app.Run();

static async IAsyncEnumerable<OrderStatus> GetStatusesAsync(int orderId, [EnumeratorCancellation] CancellationToken cancellationToken)
{
    string[] statuses = ["Received", "Packed", "Shipped"];
    foreach (string status in statuses)
    {
        await Task.Delay(1000, cancellationToken);
        yield return new OrderStatus(orderId, status);
    }
}
