namespace Bookstore.Payments;

public class PaymentFeeCalculator
{
    public decimal Fee(decimal amount, string provider) =>
        Math.Round(amount * ProviderFeeRate(provider), 2, MidpointRounding.AwayFromZero);

    // Real rates come from the provider contract table
    public virtual decimal ProviderFeeRate(string provider) => provider switch
    {
        "card-gateway" => 0.025m,
        "bank-transfer" => 0.0m,
        _ => throw new KeyNotFoundException($"No fee contract for provider '{provider}'."),
    };
}
