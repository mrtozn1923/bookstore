namespace Bookstore.Institutional;

public sealed record Institution(string Id, string Name, string TaxNumber);

// Institutional customers known to the samples (fake tax numbers).
public sealed class InstitutionDirectory
{
    private readonly Dictionary<string, Institution> _institutions = new()
    {
        ["school-0042"] = new("school-0042", "School 0042", "4200420042"),
        // A typo in the back office: the tax number is too short.
        ["library-0007"] = new("library-0007", "Library 0007", "70070")
    };

    public Institution? Find(string id) => _institutions.GetValueOrDefault(id);
}
