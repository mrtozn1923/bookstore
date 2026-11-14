using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Polly;
using Polly.Registry;

namespace Bookstore.Notifications;

// SMTP is not HTTP: no IHttpClientFactory, so the resilience pipeline is applied by hand.
public sealed class SmtpEmailSender(
    ResiliencePipelineProvider<string> pipelines,
    IOptions<SmtpOptions> options,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public const string PipelineName = "smtp";

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(options.Value.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        var pipeline = pipelines.GetPipeline(PipelineName);
        await pipeline.ExecuteAsync(async token =>
        {
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(options.Value.Host, options.Value.Port, SecureSocketOptions.None, token);
            await smtp.SendAsync(message, token);
            await smtp.DisconnectAsync(quit: true, token);
        }, cancellationToken);

        logger.LogInformation("E-mail '{Subject}' sent", subject);
    }
}
