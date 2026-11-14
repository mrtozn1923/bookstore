using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Bookstore.ResilienceLab.Demos;

// A tiny programmable server: answers the first requests with the given status codes, then 200.
public sealed class Flaky : IDisposable
{
    private readonly WireMockServer _server = WireMockServer.Start();
    private int _attempts;

    public Flaky(int[] statuses, TimeSpan? delay = null, IDictionary<string, string>? headers = null)
    {
        _server.Given(Request.Create().UsingAnyMethod())
            .RespondWith(Response.Create().WithCallback(_ =>
            {
                var n = Interlocked.Increment(ref _attempts);
                if (delay is { } d)
                {
                    Thread.Sleep(d);
                }

                var status = n <= statuses.Length ? statuses[n - 1] : 200;
                var message = new WireMock.ResponseMessage { StatusCode = status };
                foreach (var (key, value) in headers ?? new Dictionary<string, string>())
                {
                    message.AddHeader(key, value);
                }

                return message;
            }));
    }

    public Uri Url => new(_server.Url! + "/");

    public int Attempts => _attempts;

    public void Dispose() => _server.Dispose();
}
