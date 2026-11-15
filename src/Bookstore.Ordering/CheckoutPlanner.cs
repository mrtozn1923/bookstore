namespace Bookstore.Ordering;

public enum DeliveryType
{
    Standard,
    Express,
    StorePickup,
}

public sealed record CheckoutRequest(CustomerType Customer, PaymentMethod Payment, DeliveryType Delivery, string Language);

public sealed record CheckoutPlan(int DispatchInBusinessDays, string ConfirmationTemplate);

public static class ConfirmationTemplates
{
    // One confirmation e-mail template per delivery type and language
    public static readonly IReadOnlySet<string> Names = new HashSet<string>
    {
        "standard.tr",
        "standard.en",
        "express.tr",
        "express.en",
        "store-pickup.tr",
        "store-pickup.en",
    };
}

public static class CheckoutPlanner
{
    public const int MaxDispatchInBusinessDays = 3;

    public static CheckoutPlan Plan(CheckoutRequest request)
    {
        int days = request.Delivery == DeliveryType.Express ? 0 : 1;
        if (request.Payment == PaymentMethod.BankTransfer)
        {
            days++; // wait for the bank transfer to clear
        }

        if (request.Customer == CustomerType.Institutional)
        {
            days++; // wait for the institution's purchase approval
        }

        string template = $"{TemplatePrefix(request.Delivery)}.{request.Language}";
        if (!ConfirmationTemplates.Names.Contains(template))
        {
            throw new InvalidOperationException($"No confirmation template named '{template}'.");
        }

        return new CheckoutPlan(days, template);
    }

    private static string TemplatePrefix(DeliveryType delivery) => delivery switch
    {
        DeliveryType.Standard => "standard",
        DeliveryType.Express => "express",
        DeliveryType.StorePickup => "store-pickup",
        _ => throw new ArgumentOutOfRangeException(nameof(delivery)),
    };
}
