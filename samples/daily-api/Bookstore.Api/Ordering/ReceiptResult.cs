using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http.Metadata;

namespace Bookstore.Api.Ordering;

public sealed class ReceiptResult(OrderDto order) : IResult, IEndpointMetadataProvider
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        StringBuilder text = new();
        text.AppendLine($"Bookstore receipt #{order.Id}");
        foreach (OrderLine line in order.Lines)
        {
            text.AppendLine(string.Create(CultureInfo.InvariantCulture, $"{line.Quantity} x {line.Title} @ {line.UnitPrice:0.00} TRY"));
        }
        text.Append(string.Create(CultureInfo.InvariantCulture, $"Total: {order.Total:0.00} TRY"));

        httpContext.Response.ContentType = "text/plain; charset=utf-8";
        await httpContext.Response.WriteAsync(text.ToString());
    }

    // OpenAPI'ye bu sonucun 200 ve text/plain olduğunu söyler
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) =>
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(string), ["text/plain"]));
}
