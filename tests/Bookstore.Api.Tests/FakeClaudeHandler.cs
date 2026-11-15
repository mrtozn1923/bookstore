using System.Net;
using System.Text;
using Anthropic;

namespace Bookstore.Api.Tests;

// Answers SDK requests with prepared JSON and records what the SDK sent; nothing leaves the process.
public sealed class FakeClaudeHandler(params string[] responses) : HttpMessageHandler
{
    private readonly Queue<string> _responses = new(responses);

    public List<string> RequestBodies { get; } = [];
    public List<HttpRequestMessage> Requests { get; } = [];

    public AnthropicClient CreateClient() =>
        new() { ApiKey = "fake-api-key-for-tests", HttpClient = new HttpClient(this), MaxRetries = 0 };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(_responses.Dequeue(), Encoding.UTF8, "application/json"),
        };
    }
}
