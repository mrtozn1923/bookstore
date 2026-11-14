namespace Bookstore.Api.Options;

public sealed class PaymentWebhookOptions
{
    public const string SectionName = "Payments:Webhook";

    public string Secret { get; set; } = "";
}
