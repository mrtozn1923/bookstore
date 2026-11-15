namespace Bookstore.Web.Institutional;

// Lives in Session between the three steps: small, flat, JSON-serializable
public sealed class InstitutionalOrderDraft
{
    public string? InstitutionName { get; set; }
    public string? UnitName { get; set; }
    public string? ContactEmail { get; set; }
    public Dictionary<int, int> Quantities { get; set; } = [];

    public int TotalCopies => Quantities.Values.Sum();
}
