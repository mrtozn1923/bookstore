namespace Bookstore.Api.Options;

public sealed class ShippingOptions
{
    public const string SectionName = "Shipping";

    public decimal FreeShippingThreshold { get; set; }
    public decimal StandardFee { get; set; }
}
