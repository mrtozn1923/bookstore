using MimeKit;
using MimeKit.Utils;

namespace Bookstore.Notifications.Email;

public static class MimeMessageFactory
{
    public static MimeMessage Create(EmailMessage message, SmtpOptions options)
    {
        MimeMessage mime = new();
        mime.From.Add(new MailboxAddress(options.FromName, options.FromAddress));
        mime.To.Add(new MailboxAddress(message.To.DisplayName ?? "", message.To.Address));
        mime.Subject = message.Subject;
        mime.Date = DateTimeOffset.UtcNow;
        mime.MessageId = MimeUtils.GenerateMessageId(MailboxAddress.Parse(options.FromAddress).Domain);

        BodyBuilder body = new()
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody,
        };

        foreach (EmailAttachment attachment in message.Attachments)
        {
            ContentType contentType = ContentType.Parse(attachment.ContentType);
            if (attachment.ContentId is null)
            {
                body.Attachments.Add(attachment.FileName, attachment.Content.ToArray(), contentType);
            }
            else
            {
                // Inline image: lives in multipart/related, referenced as cid: from the HTML
                MimeEntity inline = body.LinkedResources.Add(attachment.FileName, attachment.Content.ToArray(), contentType);
                inline.ContentId = attachment.ContentId;
            }
        }

        mime.Body = body.ToMessageBody();
        return mime;
    }
}
