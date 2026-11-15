using System.Runtime.CompilerServices;
using Bookstore.Bcl.Text;

namespace Bookstore.Bcl.Orders;

// Project-specific guards for checks the BCL does not have, in the same style as the built-in helpers.
public static class Guard
{
    public static Isbn NotDefault(Isbn value, [CallerArgumentExpression(nameof(value))] string? paramName = null) =>
        value == default ? throw new ArgumentException("An ISBN is required.", paramName) : value;

    public static string Email(string value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
        int at = value.IndexOf('@');
        if (at <= 0 || at != value.LastIndexOf('@') || at == value.Length - 1)
            throw new ArgumentException($"'{value}' is not an e-mail address.", paramName);
        return value;
    }
}
