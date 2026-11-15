using System.Diagnostics;
using System.Globalization;
using System.Net.Security;
using System.Security.Cryptography;
using Bookstore.Notifications;
using Bookstore.Notifications.Documents;
using Bookstore.Notifications.Email;
using Bookstore.Notifications.Samples;
using Bookstore.Notifications.Templates;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddNotifications(builder.Configuration);
builder.Services.AddSingleton<IDocumentRenderer<InvoiceModel>, PlainTextInvoiceRenderer>();
using IHost host = builder.Build();

string command = args.FirstOrDefault() ?? "order-confirmation";
SmtpOptions smtp = host.Services.GetRequiredService<IOptions<SmtpOptions>>().Value;

switch (command)
{
    case "order-confirmation":
        await SendOrderConfirmationAsync(host.Services);
        break;
    case "connect":
        // Try one connection with the given security mode, without the certificate pin
        await TryConnectAsync(smtp, Enum.Parse<SecureSocketOptions>(args[1]), pin: args.Contains("--pin"), authenticate: args.Contains("--auth"));
        break;
    case "reminder":
        await SendReminderAsync(host.Services, args.Length > 1 ? args[1] : "tr-TR", printHtml: args.Contains("--html"));
        break;
    case "timing":
        await CompareConnectionReuseAsync(host.Services, smtp, int.Parse(args[1]));
        break;
}

static async Task SendOrderConfirmationAsync(IServiceProvider services)
{
    InvoiceModel invoice = BookstoreSampleData.Invoice(1042);
    IDocumentRenderer<InvoiceModel> renderer = services.GetRequiredService<IDocumentRenderer<InvoiceModel>>();
    using MemoryStream invoiceFile = new();
    await renderer.RenderAsync(invoice, invoiceFile);

    byte[] logo = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "assets", "logo.png"));
    EmailMessage message = OrderConfirmationEmail.Create(invoice, logo,
        new EmailAttachment($"invoice-{invoice.InvoiceNumber}{renderer.Format.Extension}", renderer.Format.ContentType, invoiceFile.ToArray()));

    IEmailSender sender = services.GetRequiredService<IEmailSender>();
    await sender.SendAsync(message);
}

static async Task SendReminderAsync(IServiceProvider services, string cultureName, bool printHtml)
{
    EmailTemplates templates = services.GetRequiredService<EmailTemplates>();
    CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
    string approver = culture.Name == "tr-TR" ? "Nevin" : "Grace";
    EmailMessage message = await templates.ApprovalReminderAsync(
        BookstoreSampleData.ApprovalReminder(approver), new EmailAddress($"{approver.ToLowerInvariant()}@example.com", approver), culture);

    Console.WriteLine($"Subject: {message.Subject}");
    Console.WriteLine(message.TextBody);
    if (printHtml)
    {
        Console.WriteLine(message.HtmlBody);
    }

    await services.GetRequiredService<IEmailSender>().SendAsync(message);
}

static async Task TryConnectAsync(SmtpOptions options, SecureSocketOptions security, bool pin, bool authenticate)
{
    using SmtpClient client = new() { Timeout = 10_000 };
    if (pin)
    {
        client.CheckCertificateRevocation = false;
        client.ServerCertificateValidationCallback = (_, certificate, _, errors) =>
            errors == SslPolicyErrors.None ||
            Convert.ToHexString(SHA256.HashData(certificate!.GetRawCertData()))
                .Equals(options.TrustedCertificateSha256!.Replace(":", ""), StringComparison.OrdinalIgnoreCase);
    }

    try
    {
        await client.ConnectAsync(options.Host, options.Port, security);
        Console.WriteLine($"{security}: connected, secure={client.IsSecure}, {client.SslProtocol}, auth: {string.Join(" ", client.AuthenticationMechanisms)}");
        if (authenticate)
        {
            await client.AuthenticateAsync(options.UserName!, options.Password!);
            Console.WriteLine($"{security}: authenticated as {options.UserName}");
        }
        await client.DisconnectAsync(true);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{security}: {ex.GetType().Name}: {ex.Message}");
    }
}

static async Task CompareConnectionReuseAsync(IServiceProvider services, SmtpOptions options, int count)
{
    EmailMessage Message(int i) => new()
    {
        To = new EmailAddress($"customer{i}@example.com"),
        Subject = $"Timing test {i}",
        HtmlBody = "<p>Timing test</p>",
        TextBody = "Timing test",
    };

    // 1) A new connection per message: connect, STARTTLS, AUTH, send, QUIT every time
    Stopwatch perMessage = Stopwatch.StartNew();
    for (int i = 0; i < count; i++)
    {
        await using MailKitEmailSender fresh = new(Options.Create(options), NullLogger<MailKitEmailSender>.Instance);
        await fresh.SendAsync(Message(i));
    }
    perMessage.Stop();

    // 2) One sender, one connection for all messages
    Stopwatch reused = Stopwatch.StartNew();
    await using (MailKitEmailSender shared = new(Options.Create(options), NullLogger<MailKitEmailSender>.Instance))
    {
        for (int i = 0; i < count; i++)
        {
            await shared.SendAsync(Message(i));
        }
    }
    reused.Stop();

    Console.WriteLine($"{count} messages, new connection each: {perMessage.ElapsedMilliseconds} ms");
    Console.WriteLine($"{count} messages, one reused connection: {reused.ElapsedMilliseconds} ms");
}
