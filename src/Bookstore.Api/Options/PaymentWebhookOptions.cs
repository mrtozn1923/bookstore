using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Options;

public sealed class PaymentWebhookOptions
{
    public const string SectionName = "Payments:Webhook";

    // Ödeme sağlayıcısının şu an imzaladığı secret
    [Required, MinLength(32)]
    public string Secret { get; set; } = "";

    // Rotation sırasında geçici olarak kabul edilen ikinci secret
    public string? SecondarySecret { get; set; }
}
