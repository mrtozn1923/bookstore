namespace Bookstore.Institutional.Domain;

public class DiscountAgreement
{
    private DiscountAgreement() { } // EF Core için

    public DiscountAgreement(int institutionId, decimal discountRate, DateOnly startDate, DateOnly? endDate)
    {
        InstitutionId = institutionId;
        DiscountRate = discountRate;
        StartDate = startDate;
        EndDate = endDate;
    }

    public int Id { get; private set; }
    public int InstitutionId { get; private set; }
    public decimal DiscountRate { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }

    public bool IsActiveOn(DateOnly day) => StartDate <= day && (EndDate is null || day <= EndDate);
}
