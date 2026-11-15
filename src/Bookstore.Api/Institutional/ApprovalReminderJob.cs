using System.Globalization;
using Bookstore.Api.Data;
using Bookstore.Notifications;
using Bookstore.Notifications.Outbox;
using Bookstore.Notifications.Templates;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Api.Institutional;

public sealed record ReminderRunResult(int Queued, int AlreadyQueued);

// Runs once a day (a recurring job in production); one reminder per approver per day
public sealed class ApprovalReminderJob(BookstoreContext db, EmailTemplates templates, EmailOutbox outbox, TimeProvider time)
{
    public async Task<ReminderRunResult> RunAsync(CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        List<PurchaseApproval> waiting = await db.PurchaseApprovals
            .Where(a => !a.IsDecided && a.RequestedAtUtc <= now.AddHours(-48))
            .OrderBy(a => a.RequestedAtUtc)
            .ToListAsync(cancellationToken);

        int queued = 0, alreadyQueued = 0;
        foreach (IGrouping<string, PurchaseApproval> approver in waiting.GroupBy(a => a.ApproverEmail))
        {
            PurchaseApproval first = approver.First();
            ApprovalReminderModel model = new(first.ApproverName, first.InstitutionName,
                [.. approver.Select(a => new PendingApproval(a.OrderId, a.RequestedBy, a.UnitName, a.Total, a.RequestedAtUtc, a.Note))],
                "https://bookstore.example.com/institution/approvals");

            EmailMessage message = await templates.ApprovalReminderAsync(model, new EmailAddress(first.ApproverEmail, first.ApproverName),
                CultureInfo.GetCultureInfo(first.ApproverCulture));

            string key = $"approval-reminder:{first.ApproverEmail}:{now:yyyy-MM-dd}";
            if (await outbox.EnqueueAsync(db, key, message, cancellationToken))
            {
                queued++;
                foreach (PurchaseApproval approval in approver)
                {
                    approval.LastRemindedAtUtc = now;
                }
            }
            else
            {
                alreadyQueued++;
            }
        }

        // The outbox rows and LastRemindedAtUtc are committed together, or not at all
        await db.SaveChangesAsync(cancellationToken);
        return new ReminderRunResult(queued, alreadyQueued);
    }
}
