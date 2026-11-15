using Bookstore.Notifications.Documents.Html;
using Bookstore.Notifications.Email;
using Bookstore.Notifications.Outbox;
using Bookstore.Notifications.Storage;
using Bookstore.Notifications.Templates;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bookstore.Notifications;

public static class NotificationsServiceCollectionExtensions
{
    public static IServiceCollection AddEmailOutbox<TContext>(this IServiceCollection services, IConfiguration configuration)
        where TContext : DbContext
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<EmailOutboxOptions>()
            .Bind(configuration.GetSection("Notifications:Outbox"))
            .PostConfigure(o => o.RetryDelays = o.RetryDelays is [] ? EmailOutboxOptions.DefaultRetryDelays : o.RetryDelays);
        services.AddSingleton<EmailOutbox>();
        services.AddSingleton<OutboxEmailDispatcher<TContext>>();
        services.AddHostedService(sp => sp.GetRequiredService<OutboxEmailDispatcher<TContext>>());
        return services;
    }

    public static IServiceCollection AddNotifications(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<LocalFileStoreOptions>()
            .Bind(configuration.GetSection("Notifications:FileStore"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IFileStore, LocalFileStore>();
        services.AddLocalization();
        services.AddLogging();
        services.AddOptions<EmailTemplateOptions>().Bind(configuration.GetSection("Notifications:Templates"));
        services.AddSingleton<EmailTemplates>();

        // HTML to PDF: one Chromium per application, started on the first document
        services.AddSingleton<RazorHtmlRenderer>();
        services.AddOptions<ChromiumPdfOptions>()
            .Bind(configuration.GetSection("Notifications:ChromiumPdf"))
            .ValidateDataAnnotations();
        services.AddSingleton<ChromiumPdfRenderer>();
        services.AddSingleton<IDocumentRenderer<InstitutionalQuoteModel>>(sp => sp.GetRequiredService<ChromiumPdfRenderer>());

        // No SMTP host configured (e.g. a fresh clone): fall back to writing e-mails to the log
        IConfigurationSection smtp = configuration.GetSection("Notifications:Smtp");
        if (string.IsNullOrEmpty(smtp["Host"]))
        {
            services.AddSingleton<IEmailSender, LoggingEmailSender>();
            return services;
        }

        services.AddOptions<SmtpOptions>()
            .Bind(smtp)
            .ValidateDataAnnotations()
            .Validate(o => o.UserName is null || !string.IsNullOrEmpty(o.Password), "Smtp:Password is required when Smtp:UserName is set.")
            .ValidateOnStart();
        services.AddSingleton<IEmailSender, MailKitEmailSender>();
        return services;
    }
}
