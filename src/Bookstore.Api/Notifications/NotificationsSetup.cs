using System.Net.Sockets;
using MailKit;
using MailKit.Net.Smtp;
using Polly;
using Polly.Retry;

namespace Bookstore.Notifications;

public static class NotificationsSetup
{
    public static IServiceCollection AddNotifications(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SmtpOptions>().Bind(configuration.GetSection("Notifications:Smtp")).ValidateOnStart();
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        // A named, non-generic pipeline in the DI-backed registry (Polly.Extensions).
        services.AddResiliencePipeline(SmtpEmailSender.PipelineName, builder => builder
            .AddTimeout(TimeSpan.FromSeconds(15))
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(1),
                // Retry only what is transient: network errors, 4xx SMTP replies, our own attempt timeout.
                ShouldHandle = new PredicateBuilder()
                    .Handle<SocketException>()
                    .Handle<IOException>()
                    .Handle<ServiceNotConnectedException>()
                    .Handle<SmtpProtocolException>()
                    .Handle<SmtpCommandException>(ex => (int)ex.StatusCode is >= 400 and < 500)
                    .Handle<Polly.Timeout.TimeoutRejectedException>()
            })
            .AddTimeout(TimeSpan.FromSeconds(4)));

        return services;
    }
}
