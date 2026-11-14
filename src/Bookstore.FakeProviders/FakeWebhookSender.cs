using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace Bookstore.FakeProviders;

// The provider's own webhook sender (Standard Webhooks headers); it knows nothing about Bookstore's code.
public static class FakeWebhookSender
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    public static async Task SendAsync(Uri url, string secret, int copies, string type, JsonObject data)
    {
        var occurred = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.ffffffzzz", CultureInfo.InvariantCulture);
        // The provider's serializer: spaces after ':' and ',' — valid JSON, but not what System.Text.Json writes.
        var body = $$$"""{"type": "{{{type}}}", "timestamp": "{{{occurred}}}", "data": {"id": "{{{data["id"]}}}", "status": "{{{data["status"]}}}", "amount": {{{data["amount"]}}}, "currency": "{{{data["currency"]}}}", "merchant_reference": "{{{data["merchant_reference"]}}}"}}""";
        var id = $"msg_{Guid.CreateVersion7():N}";
        for (var copy = 0; copy < copies; copy++)
        {
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
                request.Headers.Add("webhook-id", id);
                request.Headers.Add("webhook-timestamp", timestamp.ToString());
                request.Headers.Add("webhook-signature", "v1," + Convert.ToBase64String(
                    HMACSHA256.HashData(Convert.FromBase64String(secret["whsec_".Length..]), Encoding.UTF8.GetBytes($"{id}.{timestamp}.{body}"))));
                try
                {
                    using var response = await Http.SendAsync(request);
                    Console.WriteLine($"Webhook {type} {id} -> {(int)response.StatusCode}");
                    if (response.IsSuccessStatusCode)
                    {
                        break;
                    }
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                {
                    Console.WriteLine($"Webhook {type} {id} -> {ex.GetType().Name}");
                }

                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
    }
}
