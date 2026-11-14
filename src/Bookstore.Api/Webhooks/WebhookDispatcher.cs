using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace Bookstore.Webhooks;

public sealed class WebhookDeliveryOptions
{
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(1);

    // Delay before attempt 2, 3, ...: the example schedule from the Standard Webhooks spec.
    public TimeSpan[] RetrySchedule { get; init; } =
    [
        TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30), TimeSpan.FromHours(2),
        TimeSpan.FromHours(5), TimeSpan.FromHours(10), TimeSpan.FromHours(14), TimeSpan.FromHours(20), TimeSpan.FromHours(24)
    ];

    // An endpoint that has failed for this long without a single success is disabled.
    public TimeSpan DisableAfter { get; init; } = TimeSpan.FromDays(5);
}

// Delivers due outbox messages, one HTTP POST each; failures go back to the outbox with a later NextAttemptAt.
public sealed class WebhookDispatcher(
    WebhookOutbox outbox,
    WebhookSubscriptionStore subscriptions,
    IHttpClientFactory httpClientFactory,
    IOptions<WebhookDeliveryOptions> options,
    TimeProvider time,
    ILogger<WebhookDispatcher> logger) : BackgroundService
{
    public const string ClientName = "webhooks";
    private const int MaxLoggedBodyBytes = 1024;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.PollInterval, time);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            // In parallel so that one slow customer does not hold the others back; the whole batch
            // finishes before the next poll, so a message is never sent twice at the same time.
            await Parallel.ForEachAsync(outbox.Due(time.GetUtcNow()),
                new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = stoppingToken },
                async (message, token) => await DeliverAsync(message, token));
        }
    }

    public async Task DeliverAsync(WebhookMessage message, CancellationToken cancellationToken)
    {
        var subscription = subscriptions.Find(message.SubscriptionId)!;
        if (subscription.Status == WebhookSubscriptionStatus.Disabled)
        {
            message.Status = WebhookMessageStatus.Failed;
            return;
        }

        var now = time.GetUtcNow();
        var timestamp = now.ToUnixTimeSeconds();
        using var request = new HttpRequestMessage(HttpMethod.Post, subscription.Url)
        {
            Content = new ByteArrayContent(message.Payload)
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Headers.Add("webhook-id", message.Id);
        request.Headers.Add("webhook-timestamp", timestamp.ToString());
        request.Headers.Add("webhook-signature", WebhookSignature.Sign(message.Id, timestamp, message.Payload, subscription.ActiveSecrets(now)));

        var started = Stopwatch.GetTimestamp();
        int? status = null;
        string? error = null;
        TimeSpan? retryAfter = null;
        try
        {
            var client = httpClientFactory.CreateClient(ClientName);
            // Headers only: we never buffer a large response body from someone else's server.
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            status = (int)response.StatusCode;
            retryAfter = response.Headers.RetryAfter?.Delta;
            if (!response.IsSuccessStatusCode)
            {
                error = await ReadStartAsync(response, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or Polly.Timeout.TimeoutRejectedException)
        {
            error = ex.Message;
        }

        message.Attempts.Add(new WebhookDeliveryAttempt(now, status, error, Stopwatch.GetElapsedTime(started)));
        if (status is >= 200 and < 300)
        {
            message.Status = WebhookMessageStatus.Delivered;
            message.ConsecutiveFailures = 0;
            subscription.FailingSince = null;
            logger.LogInformation("Webhook {MessageId} delivered to {Url} on attempt {Attempt}", message.Id, subscription.Url, message.Attempts.Count);
            return;
        }

        subscription.FailingSince ??= now;
        if (status == (int)HttpStatusCode.Gone || now - subscription.FailingSince >= options.Value.DisableAfter)
        {
            Disable(subscription, status == (int)HttpStatusCode.Gone ? "Endpoint answered 410 Gone." : "Endpoint kept failing.");
            message.Status = WebhookMessageStatus.Failed;
            return;
        }

        var schedule = options.Value.RetrySchedule;
        var failures = ++message.ConsecutiveFailures;
        if (failures > schedule.Length)
        {
            message.Status = WebhookMessageStatus.Failed;
            logger.LogWarning("Webhook {MessageId} failed after {Attempts} attempts", message.Id, failures);
            return;
        }

        // Up to 10% jitter so that endpoints recovering from an outage are not hit all at once.
        var delay = schedule[failures - 1] * (1 + Random.Shared.NextDouble() * 0.1);
        if (retryAfter > delay)
        {
            delay = retryAfter.Value;
        }

        message.NextAttemptAt = now + delay;
        logger.LogWarning("Webhook {MessageId} attempt {Attempt} failed ({Status}), next attempt in {Delay}",
            message.Id, failures, status?.ToString() ?? error, delay);
    }

    private void Disable(WebhookSubscription subscription, string reason)
    {
        subscription.Status = WebhookSubscriptionStatus.Disabled;
        subscription.DisabledReason = reason;
        // In Bookstore this also e-mails the institution's technical contact.
        logger.LogWarning("Webhook subscription {SubscriptionId} disabled: {Reason}", subscription.Id, reason);
    }

    private static async Task<string> ReadStartAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var buffer = new byte[MaxLoggedBodyBytes];
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var read = await stream.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false, cancellationToken);
        return System.Text.Encoding.UTF8.GetString(buffer, 0, read);
    }
}
