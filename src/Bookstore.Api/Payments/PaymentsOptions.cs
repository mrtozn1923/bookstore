using System.ComponentModel.DataAnnotations;

namespace Bookstore.Payments;

// Bound to the "Payments" section. TimeoutSeconds and CheckoutEnabled are incident levers: they are read on every
// checkout through IOptionsMonitor, so a change in switches.json takes effect without a restart or a deploy.
public sealed class PaymentsOptions
{
    public const string SectionName = "Payments";

    [Required]
    public Uri? BaseAddress { get; set; }

    // How long one authorization may take before the checkout gives up and answers 502.
    [Range(1, 30)]
    public int TimeoutSeconds { get; set; } = 3;

    // Kill switch: false stops checkouts before an order is created (503 + Retry-After, the cart is kept).
    public bool CheckoutEnabled { get; set; } = true;
}
