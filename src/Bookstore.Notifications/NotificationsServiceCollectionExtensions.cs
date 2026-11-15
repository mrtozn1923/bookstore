using Bookstore.Notifications.Documents.Html;
using Bookstore.Notifications.Email;
using Bookstore.Notifications.Images;
using Bookstore.Notifications.Outbox;
using Bookstore.Notifications.Storage;
using Bookstore.Notifications.Templates;
using Amazon;
using Amazon.S3;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
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

    // Notifications:FileStore:Provider = Local (default) | AzureBlob | S3
    public static IServiceCollection AddFileStore(this IServiceCollection services, IConfigurationSection section)
    {
        switch (section["Provider"] ?? "Local")
        {
            case "AzureBlob":
                // The SDK retries on its own; keep that short so a job's own retry (Hangfire) decides about longer outages
                BlobClientOptions blobOptions = new()
                {
                    Retry = { MaxRetries = 2, Delay = TimeSpan.FromSeconds(1), NetworkTimeout = TimeSpan.FromSeconds(10) },
                };
                services.AddSingleton(_ => new BlobContainerClient(section["Azure:ConnectionString"], section["Azure:ContainerName"] ?? "bookstore", blobOptions));
                services.AddSingleton<AzureBlobFileStore>();
                services.AddSingleton<IFileStore>(sp => sp.GetRequiredService<AzureBlobFileStore>());
                services.AddSingleton<IDirectUploadStore>(sp => sp.GetRequiredService<AzureBlobFileStore>());
                break;
            case "S3":
                services.AddOptions<S3FileStoreOptions>().Bind(section.GetSection("S3")).ValidateDataAnnotations().ValidateOnStart();
                services.AddSingleton<IAmazonS3>(sp =>
                {
                    S3FileStoreOptions o = sp.GetRequiredService<IOptions<S3FileStoreOptions>>().Value;
                    AmazonS3Config config = string.IsNullOrEmpty(o.ServiceUrl)
                        ? new() { RegionEndpoint = RegionEndpoint.GetBySystemName(o.Region) }
                        : new() { ServiceURL = o.ServiceUrl, ForcePathStyle = true, AuthenticationRegion = o.Region };
                    // Access key and secret come from the default AWS credential chain (environment, profile, IAM role)
                    return new AmazonS3Client(config);
                });
                services.AddSingleton<S3FileStore>();
                services.AddSingleton<IFileStore>(sp => sp.GetRequiredService<S3FileStore>());
                services.AddSingleton<IDirectUploadStore>(sp => sp.GetRequiredService<S3FileStore>());
                break;
            default:
                services.AddOptions<LocalFileStoreOptions>().Bind(section).ValidateDataAnnotations().ValidateOnStart();
                services.AddSingleton<LocalFileStore>();
                services.AddSingleton<IFileStore>(sp => sp.GetRequiredService<LocalFileStore>());
                break;
        }

        return services;
    }

    public static IServiceCollection AddNotifications(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddFileStore(configuration.GetSection("Notifications:FileStore"));
        services.AddLocalization();
        services.AddLogging();
        services.AddOptions<EmailTemplateOptions>().Bind(configuration.GetSection("Notifications:Templates"));
        services.AddSingleton<EmailTemplates>();

        services.AddOptions<CoverImageOptions>()
            .Bind(configuration.GetSection("Notifications:CoverImages"))
            .PostConfigure(o => o.Widths = o.Widths is [] ? CoverImageOptions.DefaultWidths : o.Widths)
            .ValidateDataAnnotations();
        services.AddSingleton<CoverImageProcessor>();

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
