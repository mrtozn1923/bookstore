namespace Bookstore.Notifications.Templates;

public sealed record PendingApproval(int OrderId, string RequestedBy, string UnitName, decimal Total, DateTime RequestedAtUtc, string? Note);

public sealed record ApprovalReminderModel(
    string ApproverName,
    string InstitutionName,
    IReadOnlyList<PendingApproval> Pending,
    string ApprovalsUrl);

// Marker type for IStringLocalizer<EmailText>; strings live in EmailText.resx / EmailText.tr.resx
public sealed class EmailText;
