using Bookstore.Notifications.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.Notifications;

internal sealed class EmailNotificationSender : INotificationSender
{
    public Task SendAsync(string to, string subject, CancellationToken ct = default)
    {
        Console.WriteLine($"  [Notifications] e-mail to {to}: {subject}");
        return Task.CompletedTask;
    }
}

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services) =>
        services.AddSingleton<INotificationSender, EmailNotificationSender>();
}
