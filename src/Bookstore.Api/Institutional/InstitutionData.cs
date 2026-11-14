namespace Bookstore.Api.Institutional;

public sealed record InstitutionUnit(string Id, string Name);

public sealed record Institution(string Id, string Name, IReadOnlyList<InstitutionUnit> Units);

public sealed class InstitutionData
{
    private readonly List<Institution> _institutions =
    [
        new("lakeside-school", "Lakeside School", [new("main-campus", "Main Campus"), new("east-campus", "East Campus")]),
        new("city-library", "City Library", [new("central", "Central Library"), new("harbor-branch", "Harbor Branch")]),
    ];

    public Institution? Find(string id) => _institutions.Find(i => i.Id == id);
}
