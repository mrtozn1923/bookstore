namespace Bookstore.Institutional;

public enum InstitutionKind
{
    School,
    Library,
    Company
}

// A school, library or company that places bulk orders (Kurumsal Sipariş).
public sealed class Institution
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public InstitutionKind Kind { get; set; }
    public List<InstitutionUnit> Units { get; } = [];
}

// Campus, branch or department; units can be nested.
public sealed class InstitutionUnit
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public int? ParentUnitId { get; set; }
    public InstitutionUnit? ParentUnit { get; set; }
    public required string Name { get; set; }
}
