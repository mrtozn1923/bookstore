using Bookstore.Privacy;
using Microsoft.Extensions.Compliance.Classification;

namespace Bookstore.Ordering;

[DataCategory("customer-profile")]
public sealed class Customer
{
    [PersonalData(InformationType = "Customer ID")]
    public int Id { get; set; }

    [PersonalData(InformationType = "Name")]
    public required string FullName { get; set; }

    [PersonalData(InformationType = "Contact Info")]
    public required string Email { get; set; }

    [PersonalData(InformationType = "Contact Info")]
    public string? Phone { get; set; }

    // Minimisation: the product only needs "is the customer an adult", not the birth date.
    [PersonalData(InformationType = "Age Confirmation")]
    public DateTime? AdultConfirmedAtUtc { get; set; }

    // Minimisation: analytics segments by city; the full address lives on the order.
    [PersonalData(InformationType = "Location")]
    public required string City { get; set; }

    [NoDataClassification]
    public DateTime CreatedAtUtc { get; set; }

    public List<Order> Orders { get; } = [];
    public List<CustomerSession> Sessions { get; } = [];
}
