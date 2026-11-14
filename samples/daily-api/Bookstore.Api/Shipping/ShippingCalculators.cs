namespace Bookstore.Api.Shipping;

public interface IShippingCalculator
{
    decimal Calculate(int quantity);
}

public class StandardShipping : IShippingCalculator
{
    public decimal Calculate(int quantity) => quantity >= 20 ? 0m : 49.90m;
}

public class ExpressShipping : IShippingCalculator
{
    public decimal Calculate(int quantity) => 99.90m + quantity * 2m;
}
