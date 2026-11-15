using Bookstore.Notifications.Email;
using Bookstore.Notifications.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.Notifications;

public static class NotificationsServiceCollectionExtensions
{
    public static IServiceCollection AddNotifications(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<LocalFileStoreOptions>()
            .Bind(configuration.GetSection("Notifications:FileStore"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IFileStore, LocalFileStore>();

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
