namespace Bookstore.Institutional.Domain;

public readonly record struct Money(decimal Amount, string Currency)
{
    public static Money Try(decimal amount) => new(amount, "TRY");

    public Money Add(Money other) => other.Currency == Currency
        ? new Money(Amount + other.Amount, Currency)
        : throw new InvalidOperationException($"Cannot add {other.Currency} to {Currency}.");
}
