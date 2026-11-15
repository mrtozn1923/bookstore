namespace Bookstore.Ordering;

public enum CustomerType
{
    Individual,
    Institutional,
    Guest,
}

public enum PaymentMethod
{
    CreditCard,
    BankTransfer,
    OnAccount,
}

public enum OrderError
{
    UnknownCustomerType,
    QuantityMustBePositive,
    BelowInstitutionalMinimum,
    PaymentMethodNotAllowed,
}

public sealed record OrderRequest(CustomerType Customer, int TotalQuantity, PaymentMethod Payment);

public static class OrderRequestValidator
{
    public static IReadOnlyList<OrderError> Validate(OrderRequest request)
    {
        if (!Enum.IsDefined(request.Customer))
        {
            return [OrderError.UnknownCustomerType];
        }

        List<OrderError> errors = [];
        if (request.TotalQuantity <= 0)
        {
            errors.Add(OrderError.QuantityMustBePositive);
        }
        else if (request.Customer == CustomerType.Institutional
                 && request.TotalQuantity < InstitutionalPricing.MinimumQuantity)
        {
            errors.Add(OrderError.BelowInstitutionalMinimum);
        }

        if (!IsAllowed(request.Customer, request.Payment))
        {
            errors.Add(OrderError.PaymentMethodNotAllowed);
        }

        return errors;
    }

    private static bool IsAllowed(CustomerType customer, PaymentMethod payment) => (customer, payment) switch
    {
        _ when !Enum.IsDefined(payment) => false,
        (_, PaymentMethod.OnAccount) => customer == CustomerType.Institutional,
        (CustomerType.Guest, _) => payment == PaymentMethod.CreditCard,
        _ => true,
    };
}
