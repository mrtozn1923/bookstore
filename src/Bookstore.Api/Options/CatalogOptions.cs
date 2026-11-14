using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Options;

public class CatalogOptions
{
    public const string SectionName = "Catalog";

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;

    [Required]
    public string Currency { get; set; } = string.Empty;

    [EmailAddress]
    public string SupportEmail { get; set; } = string.Empty;

    public string PricingApiKey { get; set; } = string.Empty;
}
