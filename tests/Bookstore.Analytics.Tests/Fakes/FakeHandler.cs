using System.IO.Compression;
using System.Net;
using System.Text;

namespace Bookstore.Analytics.Tests.Fakes;

// Answers HTTP calls in-process and records what was sent.
public sealed class FakeHandler(Func<HttpRequestMessage, string, (HttpStatusCode, string)> respond) : HttpMessageHandler
{
    public List<(HttpRequestMessage Request, string Body)> Calls { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        string body = request.Content is null ? "" : await ReadBodyAsync(request.Content, ct);
        Calls.Add((request, body));
        (HttpStatusCode status, string json) = respond(request, body);
        return new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    // The Google client gzips request bodies by default.
    private static async Task<string> ReadBodyAsync(HttpContent content, CancellationToken ct)
    {
        byte[] bytes = await content.ReadAsByteArrayAsync(ct);
        if (!content.Headers.ContentEncoding.Contains("gzip"))
        {
            return Encoding.UTF8.GetString(bytes);
        }

        await using GZipStream gzip = new(new MemoryStream(bytes), CompressionMode.Decompress);
        using StreamReader reader = new(gzip, Encoding.UTF8);
        return await reader.ReadToEndAsync(ct);
    }
}

public sealed class FakeGoogleHttpClientFactory(HttpMessageHandler handler) : Google.Apis.Http.HttpClientFactory
{
    protected override HttpMessageHandler CreateHandler(Google.Apis.Http.CreateHttpClientArgs args) => handler;
}
