// Sends a signed "payment.succeeded" webhook, the way the payment provider would.
// Usage: dotnet run scripts/send-payment-webhook.cs -- <webhook url> <order id> [--tamper] [--age <seconds>] [--event <id>]
// The secret comes from PAYMENT_WEBHOOK_SECRET (Development value: appsettings.Development.json).
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

string url = args[0];
int orderId = int.Parse(args[1], CultureInfo.InvariantCulture);
bool tamper = args.Contains("--tamper");
int age = OptionValue("--age") is { } a ? int.Parse(a, CultureInfo.InvariantCulture) : 0;
string eventId = OptionValue("--event") ?? $"evt-{Guid.CreateVersion7():N}"[..16];
string secret = Environment.GetEnvironmentVariable("PAYMENT_WEBHOOK_SECRET")
    ?? throw new InvalidOperationException("Set PAYMENT_WEBHOOK_SECRET.");

string body = $$"""{"eventId":"{{eventId}}","type":"payment.succeeded","orderId":{{orderId}},"amount":659.80,"currency":"TRY"}""";
long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - age;
byte[] key = Encoding.UTF8.GetBytes(secret);
byte[] signed = Encoding.UTF8.GetBytes($"{timestamp}.{body}");
string signature = Convert.ToHexStringLower(HMACSHA256.HashData(key, signed));

// --tamper changes the amount after signing, as an attacker in the middle would.
string sent = tamper ? body.Replace("659.80", "1.00") : body;

using HttpClient http = new();
using HttpRequestMessage request = new(HttpMethod.Post, url)
{
    Content = new StringContent(sent, Encoding.UTF8, "application/json")
};
request.Headers.Add("X-Payment-Timestamp", timestamp.ToString(CultureInfo.InvariantCulture));
request.Headers.Add("X-Payment-Signature", $"v1={signature}");

using HttpResponseMessage response = await http.SendAsync(request);
Console.WriteLine($"{eventId} -> {(int)response.StatusCode} {response.ReasonPhrase}");
return response.IsSuccessStatusCode ? 0 : 1;

string? OptionValue(string name) =>
    Array.IndexOf(args, name) is var i and >= 0 && i + 1 < args.Length ? args[i + 1] : null;
