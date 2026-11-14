namespace Bookstore.Institutional.Domain;

public class InstitutionUser
{
    private InstitutionUser() { } // EF Core için

    public InstitutionUser(string staffNumber, string fullName, string email, int institutionUnitId, int? approverId)
    {
        StaffNumber = staffNumber;
        FullName = fullName;
        Email = email;
        InstitutionUnitId = institutionUnitId;
        ApproverId = approverId;
    }

    public int Id { get; private set; }
    public string StaffNumber { get; private set; } = "";
    public string FullName { get; private set; } = "";
    public string Email { get; private set; } = "";
    public int InstitutionUnitId { get; private set; }
    public InstitutionUnit InstitutionUnit { get; private set; } = null!;
    public int? ApproverId { get; private set; }
    public InstitutionUser? Approver { get; private set; }
}
