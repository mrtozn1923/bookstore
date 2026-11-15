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
        services.AddSingleton<IEmailSender, LoggingEmailSender>();
        return services;
    }
}
