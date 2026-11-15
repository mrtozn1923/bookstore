namespace Bookstore.Fp.CSharp15;

public sealed record Ok<T>(T Value);
public sealed record Failed(IReadOnlyList<OrderError> Errors);

public union Result<T>(Ok<T>, Failed)
{
    public Result<TOut> Map<TOut>(Func<T, TOut> map) => this switch
    {
        Ok<T>(var value) => new Ok<TOut>(map(value)),
        Failed failed => failed,
        null => throw new InvalidOperationException("default(Result<T>) has no value.")
    };

    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> bind) => this switch
    {
        Ok<T>(var value) => bind(value),
        Failed failed => failed,
        null => throw new InvalidOperationException("default(Result<T>) has no value.")
    };
}

public sealed record EmailInvalid(string Email);
public sealed record BookNotFound(string Isbn);
public sealed record StockInsufficient(string Isbn, int Left);

public union OrderError(EmailInvalid, BookNotFound, StockInsufficient);

public static class OrderErrors
{
    public static int StatusCode(OrderError error) => error switch
    {
        EmailInvalid => 400,
        BookNotFound => 404,
        StockInsufficient => 409
    };

    public static string Message(OrderError error) => error switch
    {
        EmailInvalid(var email) => $"'{email}' is not a valid email address.",
        BookNotFound(var isbn) => $"No book with ISBN {isbn}.",
        StockInsufficient(var isbn, var left) => $"Only {left} left for {isbn}."
    };
}
