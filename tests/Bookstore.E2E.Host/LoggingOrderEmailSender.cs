using Bookstore.Api.Integrations;
using Microsoft.Extensions.Logging;

namespace Bookstore.E2E.Host;

// E-posta gönderimi integration testlerinde Mailpit ile doğrulanıyor; E2E'de yalnız log'a yazılır
public sealed class LoggingOrderEmailSender(ILogger<LoggingOrderEmailSender> logger) : IOrderEmailSender
{
    public Task SendOrderConfirmationAsync(string to, string orderReference, decimal total, CancellationToken cancellationToken)
    {
        logger.LogInformation("E2E: confirmation for {OrderReference} to {To}, total {Total}", orderReference, to, total);
        return Task.CompletedTask;
    }
}
