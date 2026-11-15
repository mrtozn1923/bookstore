using Microsoft.Extensions.Compliance.Classification;

namespace Bookstore.Privacy;

public static class BookstoreTaxonomy
{
    public static string Name => "Bookstore";

    // Identifies a person directly or through another record (name, e-mail, address, IP, device ID).
    public static DataClassification PersonalData => new(Name, nameof(PersonalData));

    // Personal data whose leak causes direct harm (national ID, payment token). Must be encrypted at rest.
    public static DataClassification SensitiveData => new(Name, nameof(SensitiveData));
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class PersonalDataAttribute() : DataClassificationAttribute(BookstoreTaxonomy.PersonalData)
{
    public string InformationType { get; init; } = "Other";
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class SensitiveDataAttribute() : DataClassificationAttribute(BookstoreTaxonomy.SensitiveData)
{
    public string InformationType { get; init; } = "Other";
}

// Links an entity to a processing activity in the inventory.
[AttributeUsage(AttributeTargets.Class)]
public sealed class DataCategoryAttribute(string category) : Attribute
{
    public string Category => category;
}
