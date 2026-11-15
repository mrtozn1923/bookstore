using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bookstore.Notifications.Outbox;

public sealed class OutboxEmailDispatcher<TContext>(
    IServiceScopeFactory scopes,
    IEmailSender sender,
    IFileStore files,
    TimeProvider time,
    IOptions<EmailOutboxOptions> options,
    ILogger<OutboxEmailDispatcher<TContext>> logger) : BackgroundService where TContext : DbContext
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(options.Value.PollInterval, time);
        do
        {
            try
            {
                await DispatchDueAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Outbox dispatch round failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task<int> DispatchDueAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        TContext db = scope.ServiceProvider.GetRequiredService<TContext>();
        DateTime now = time.GetUtcNow().UtcDateTime;

        List<OutboxEmail> due = await db.Set<OutboxEmail>()
            .Where(e => e.Status == OutboxEmailStatus.Pending && e.NextAttemptAtUtc <= now)
            .OrderBy(e => e.NextAttemptAtUtc)
            .Take(options.Value.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (OutboxEmail email in due)
        {
            email.Attempts++;
            try
            {
                await sender.SendAsync(await ToMessageAsync(email, cancellationToken), cancellationToken);
                email.Status = OutboxEmailStatus.Sent;
                email.SentAtUtc = time.GetUtcNow().UtcDateTime;
                email.LastError = null;
                logger.LogInformation("Outbox {Key} sent on attempt {Attempt}", email.IdempotencyKey, email.Attempts);
            }
            catch (EmailDeliveryException ex)
            {
                Reschedule(email, ex);
            }
            catch (FileNotFoundException ex)
            {
                // The attachment is gone: retrying will not bring it back
                Reschedule(email, new EmailDeliveryException(ex.Message, isTransient: false, ex));
            }

            // Save after every email: a crash must not resend the ones already sent
            await db.SaveChangesAsync(cancellationToken);
        }

        return due.Count;
    }

    private async Task<EmailMessage> ToMessageAsync(OutboxEmail email, CancellationToken cancellationToken)
    {
        EmailMessage message = email.ToMessage();
        if (email.AttachmentPath is null)
        {
            return message;
        }

        using MemoryStream content = new();
        await using (Stream stored = await files.OpenReadAsync(email.AttachmentPath, cancellationToken))
        {
            await stored.CopyToAsync(content, cancellationToken);
        }

        return message with { Attachments = [new EmailAttachment(email.AttachmentName!, email.AttachmentContentType!, content.ToArray())] };
    }

    private void Reschedule(OutboxEmail email, EmailDeliveryException ex)
    {
        TimeSpan[] delays = options.Value.RetryDelays;
        email.LastError = ex.Message;
        if (!ex.IsTransient || email.Attempts > delays.Length)
        {
            email.Status = OutboxEmailStatus.Failed;
            logger.LogError("Outbox {Key} failed permanently after {Attempts} attempts: {Error}", email.IdempotencyKey, email.Attempts, ex.Message);
            return;
        }

        email.NextAttemptAtUtc = time.GetUtcNow().UtcDateTime + delays[email.Attempts - 1];
        logger.LogWarning("Outbox {Key} attempt {Attempt} failed ({Error}); next attempt at {Next:HH:mm:ss}",
            email.IdempotencyKey, email.Attempts, ex.Message, email.NextAttemptAtUtc);
    }
}
