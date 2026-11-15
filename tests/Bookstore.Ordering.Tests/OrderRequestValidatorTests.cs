namespace Bookstore.Ordering.Tests;

public sealed class OrderRequestValidatorTests
{
    // Each Choice: every valid partition at least once (EP1-EP3, EP6, EP7, EP9-EP11)
    public static TheoryData<CustomerType, int, PaymentMethod> ValidPartitions => new()
    {
        { CustomerType.Individual, 5, PaymentMethod.BankTransfer },
        { CustomerType.Institutional, 25, PaymentMethod.OnAccount },
        { CustomerType.Guest, 3, PaymentMethod.CreditCard },
    };

    [Theory]
    [MemberData(nameof(ValidPartitions))]
    public void Valid_partitions_are_accepted(CustomerType customer, int quantity, PaymentMethod payment)
    {
        IReadOnlyList<OrderError> errors = OrderRequestValidator.Validate(new OrderRequest(customer, quantity, payment));

        Assert.Empty(errors);
    }

    // One invalid partition per test; all other values come from valid partitions
    public static IEnumerable<TheoryDataRow<CustomerType, int, PaymentMethod, OrderError>> InvalidPartitions =>
    [
        new(CustomerType.Individual, -2, PaymentMethod.CreditCard, OrderError.QuantityMustBePositive) { Label = "EP5" },
        new(CustomerType.Institutional, 12, PaymentMethod.OnAccount, OrderError.BelowInstitutionalMinimum) { Label = "EP8" },
        new(CustomerType.Individual, 5, PaymentMethod.OnAccount, OrderError.PaymentMethodNotAllowed) { Label = "EP12" },
        new(CustomerType.Guest, 3, PaymentMethod.BankTransfer, OrderError.PaymentMethodNotAllowed) { Label = "EP13" },
        new((CustomerType)7, 5, PaymentMethod.CreditCard, OrderError.UnknownCustomerType) { Label = "EP4" },
        new(CustomerType.Individual, 5, (PaymentMethod)9, OrderError.PaymentMethodNotAllowed) { Label = "EP14" },
    ];

    [Theory]
    [MemberData(nameof(InvalidPartitions))]
    public void Each_invalid_partition_is_rejected_with_its_own_error(
        CustomerType customer, int quantity, PaymentMethod payment, OrderError expected)
    {
        IReadOnlyList<OrderError> errors = OrderRequestValidator.Validate(new OrderRequest(customer, quantity, payment));

        Assert.Equal([expected], errors);
    }

    // Output partition: multiple errors are reported together
    [Fact]
    public void All_errors_are_reported_together()
    {
        IReadOnlyList<OrderError> errors = OrderRequestValidator.Validate(
            new OrderRequest(CustomerType.Guest, 0, PaymentMethod.BankTransfer));

        Assert.Equal([OrderError.QuantityMustBePositive, OrderError.PaymentMethodNotAllowed], errors);
    }
}
