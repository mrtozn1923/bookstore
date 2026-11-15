using System.Net;
using System.Text;
using Anthropic;

namespace Bookstore.Api.Tests;

public sealed record FakeResponse(
    string Body,
    string ContentType = "application/json",
    HttpStatusCode Status = HttpStatusCode.OK,
    IReadOnlyDictionary<string, string>? Headers = null);

// Answers SDK requests with prepared responses and records what the SDK sent; nothing leaves the process.
public sealed class FakeClaudeHandler : HttpMessageHandler
{
    private readonly Queue<FakeResponse> _responses;

    public FakeClaudeHandler(params string[] responses) => _responses = new(responses.Select(body => new FakeResponse(body)));

    public FakeClaudeHandler(params FakeResponse[] responses) => _responses = new(responses);

    public List<string> RequestBodies { get; } = [];
    public List<HttpRequestMessage> Requests { get; } = [];

    public AnthropicClient CreateClient() =>
        new() { ApiKey = "fake-api-key-for-tests", HttpClient = new HttpClient(this), MaxRetries = 0 };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));

        FakeResponse next = _responses.Dequeue();
        HttpResponseMessage response = new(next.Status)
        {
            Content = new StringContent(next.Body, Encoding.UTF8, next.ContentType),
        };
        foreach ((string name, string value) in next.Headers ?? new Dictionary<string, string>())
        {
            response.Headers.TryAddWithoutValidation(name, value);
        }

        return response;
    }
}
