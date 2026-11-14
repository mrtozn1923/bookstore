using Xunit.Sdk;

namespace Bookstore.Ordering.Tests;

// Keeps a check that repeats across Bookstore tests in one place
internal static class OrderAssert
{
    public static OrderLine HasLine(Order order, string isbn, int quantity)
    {
        OrderLine? line = order.Lines.FirstOrDefault(l => l.Isbn == isbn);
        if (line is null || line.Quantity != quantity)
        {
            throw new OrderAssertException(
                $"OrderAssert.HasLine() Failure: {order.OrderNumber} has no line {isbn} x{quantity}" + Environment.NewLine +
                $"Lines: {string.Join(", ", order.Lines.Select(l => $"{l.Isbn} x{l.Quantity}"))}");
        }

        return line;
    }
}

public sealed class OrderAssertException(string message) : XunitException(message);
