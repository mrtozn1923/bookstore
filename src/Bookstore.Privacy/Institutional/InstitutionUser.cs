using Bookstore.Privacy;
using Microsoft.Extensions.Compliance.Classification;

namespace Bookstore.Institutional;

[DataCategory("institution-user")]
public sealed class InstitutionUser
{
    [PersonalData(InformationType = "Customer ID")]
    public int Id { get; set; }

    [NoDataClassification]
    public int InstitutionId { get; set; }

    [PersonalData(InformationType = "Name")]
    public required string FullName { get; set; }

    [PersonalData(InformationType = "Contact Info")]
    public required string WorkEmail { get; set; }

    [NoDataClassification]
    public required string Role { get; set; }
}
