using NSubstitute.Extensions;

namespace Bookstore.Payments.Tests;

public sealed class PaymentFeeCalculatorTests
{
    [Fact]
    public void Fee_uses_the_rate_of_a_new_provider()
    {
        PaymentFeeCalculator calculator = Substitute.ForPartsOf<PaymentFeeCalculator>();
        calculator.Configure().ProviderFeeRate("wallet").Returns(0.015m);

        Assert.Equal(3.00m, calculator.Fee(200m, "wallet"));
        Assert.Equal(5.00m, calculator.Fee(200m, "card-gateway"));
    }
}
