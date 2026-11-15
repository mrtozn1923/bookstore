using Bookstore.Ordering.Functional;
using Bookstore.Ordering.Orders;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Bookstore.Fp.Api;

public sealed record OrderResponse(string OrderNo, string Isbn, int Quantity, decimal Total);

public static class ResultHttp
{
    // Business failures that are not about a form field
    private static readonly Dictionary<string, int> StatusByCode = new()
    {
        ["book.not_found"] = StatusCodes.Status404NotFound,
        ["stock.insufficient"] = StatusCodes.Status409Conflict
    };

    public static Results<Created<OrderResponse>, ValidationProblem, ProblemHttpResult> ToHttpResult(this Result<PlacedOrder> result) =>
        result.Match<Results<Created<OrderResponse>, ValidationProblem, ProblemHttpResult>>(
            order => TypedResults.Created(
                $"/orders/{order.OrderNo}",
                new OrderResponse(order.OrderNo, order.Line.Isbn, order.Line.Quantity, order.Totals.Total)),
            errors => StatusByCode.TryGetValue(errors[0].Code, out int status)
                ? TypedResults.Problem(title: errors[0].Message, statusCode: status, type: $"https://bookstore.example.com/errors/{errors[0].Code}")
                : TypedResults.ValidationProblem(errors
                    .GroupBy(error => error.Code.Split('.')[0])
                    .ToDictionary(group => group.Key, group => group.Select(error => error.Message).ToArray())));
}
