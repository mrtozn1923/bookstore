using System.ComponentModel.DataAnnotations;
using Bookstore.Web.Catalog;

namespace Bookstore.Web.Institutional;

public sealed class InstitutionStep
{
    [Required, StringLength(120)]
    public string InstitutionName { get; set; } = "";

    [StringLength(80)]
    public string? UnitName { get; set; }

    [Required, EmailAddress]
    public string ContactEmail { get; set; } = "";
}

public sealed class BooksStep
{
    public Dictionary<int, int> Quantities { get; set; } = [];

    public IReadOnlyList<Book> Catalog { get; set; } = [];
}

public sealed record ReviewStep(InstitutionalOrderDraft Draft, InstitutionalOrderSummary Summary);
