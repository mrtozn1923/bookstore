using WireMock;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Types;
using WireMock.Util;

namespace Bookstore.FakeProviders;

// An institutional customer's webhook endpoint: prints what arrives and answers according to the mode.
public static class FakeWebhookReceiver
{
    public const string Path = "/webhooks/bookstore";

    public static WireMockServer Start(int? port = null, FakeMode mode = FakeMode.Normal, Action<string>? log = null)
    {
        var server = port is null ? WireMockServer.Start() : WireMockServer.Start(port.Value);
        Configure(server, mode, log);
        return server;
    }

    public static void Configure(WireMockServer server, FakeMode mode, Action<string>? log = null)
    {
        server.Reset();
        server.Given(Request.Create().WithPath(Path).UsingPost())
            .RespondWith(Response.Create().WithCallback(async request =>
            {
                string Header(string name) => request.Headers?.TryGetValue(name, out var v) == true ? v[0] : "";
                log?.Invoke($"webhook-id: {Header("webhook-id")}\nwebhook-timestamp: {Header("webhook-timestamp")}\nwebhook-signature: {Header("webhook-signature")}\n{request.Body}");
                var status = mode switch
                {
                    FakeMode.Error => 500,
                    FakeMode.Gone => 410,
                    _ => 200
                };
                if (mode == FakeMode.Slow)
                {
                    await Task.Delay(TimeSpan.FromSeconds(20));
                }

                return new ResponseMessage
                {
                    StatusCode = status,
                    BodyData = new BodyData { DetectedBodyType = BodyType.String, BodyAsString = status == 200 ? "ok" : "receiver error" }
                };
            }));
    }
}
