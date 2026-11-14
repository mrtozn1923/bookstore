namespace Bookstore.Api.Institutional;

public record InstitutionUnit(int Id, string InstitutionName, string Name);

public class InstitutionDirectory
{
    private readonly Dictionary<int, InstitutionUnit> _units = new()
    {
        [101] = new(101, "Riverside School", "Main Campus"),
        [102] = new(102, "Riverside School", "Library"),
    };

    public InstitutionUnit? Find(int id) => _units.GetValueOrDefault(id);
}
