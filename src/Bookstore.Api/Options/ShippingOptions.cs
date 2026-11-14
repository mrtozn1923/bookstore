using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Options;

public sealed class ShippingOptions
{
    public const string SectionName = "Shipping";

    [Range(typeof(decimal), "0", "100000")]
    public decimal FreeShippingThreshold { get; set; }

    [Range(typeof(decimal), "0", "1000")]
    public decimal StandardFee { get; set; }
}
