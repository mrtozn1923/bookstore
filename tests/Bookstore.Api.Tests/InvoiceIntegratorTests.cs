using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bookstore.FakeProviders;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Bookstore.Api.Tests;

// The integrator's SOAP service imitated by WireMock: canned envelopes, copied from the fake integrator's real answers.
public sealed class InvoiceIntegratorTests : IDisposable
{
    private const string Path = "/EInvoiceService.svc";
    private const string Ns = "http://einvoice.example.com/integrator/v1";

    private readonly WireMockServer _payments = FakePaymentProvider.Start(null, new FakePaymentSettings());
    private readonly WireMockServer _carrier = FakeLegacyCarrier.Start();
    private readonly WireMockServer _integrator = WireMockServer.Start();

    public void Dispose()
    {
        _payments.Dispose();
        _carrier.Dispose();
        _integrator.Dispose();
    }

    private BookstoreApiFactory CreateApi() => new(_payments, _carrier, settings: new Dictionary<string, string?>
    {
        ["Invoicing:EInvoice:Endpoint"] = _integrator.Url + Path,
        ["Invoicing:EInvoice:Security"] = "None",
        ["Invoicing:EInvoice:TrustedServerCertificatePath"] = "",
        ["Invoicing:EInvoice:SendTimeout"] = "00:00:01",
        ["Invoicing:EInvoice:LogMessages"] = "false"
    });

    private static IRequestBuilder Operation(string name) =>
        Request.Create().WithPath(Path).UsingPost().WithHeader("SOAPAction", $"\"{Ns}/IEInvoiceService/{name}\"");

    private static IResponseBuilder Soap(string body, int status = 200) => Response.Create()
        .WithStatusCode(status)
        .WithHeader("Content-Type", "text/xml; charset=utf-8")
        .WithBody($"""<s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/"><s:Body>{body}</s:Body></s:Envelope>""");

    // SOAP 1.1: a fault always comes with HTTP 500.
    private static IResponseBuilder Fault(string code, string message) => Soap(
        $"""<s:Fault><faultcode>s:{code}</faultcode><faultstring>{message}</faultstring><detail><IntegratorFault xmlns="{Ns}"><Code>{code}</Code><Message>{message}</Message></IntegratorFault></detail></s:Fault>""",
        500);

    private static readonly string Receipt =
        $"""<SendInvoiceResponse xmlns="{Ns}"><SendInvoiceResult><Uuid>u</Uuid><EnvelopeId>ENV-9001</EnvelopeId><Status>QUEUED</Status><ReceivedAt>2026-11-15T08:00:00Z</ReceivedAt></SendInvoiceResult></SendInvoiceResponse>""";

    private int Requests(string operation) =>
        _integrator.LogEntries.Count(e => e.RequestMessage?.Headers?["SOAPAction"].Single().Contains(operation) == true);

    [Fact]
    public async Task Business_fault_is_returned_as_422_and_never_retried()
    {
        _integrator.Given(Operation("SendInvoice")).RespondWith(Fault("INVALID_TAX_NUMBER", "Receiver tax number must be 10 or 11 digits."));
        await using var api = CreateApi();

        using var response = await api.CreateClient().PostAsync("/api/orders/1004/invoice", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("INVALID_TAX_NUMBER", problem.GetProperty("integratorCode").GetString());
        Assert.Equal(1, Requests("SendInvoice"));
    }

    [Fact]
    public async Task Timeout_then_duplicate_uuid_is_treated_as_success()
    {
        // First answer is later than the 1 s send timeout; the integrator has already stored the invoice.
        _integrator.Given(Operation("SendInvoice")).InScenario("send").WillSetStateTo("stored")
            .RespondWith(Soap(Receipt).WithDelay(TimeSpan.FromSeconds(3)));
        _integrator.Given(Operation("SendInvoice")).InScenario("send").WhenStateIs("stored")
            .RespondWith(Fault("DUPLICATE_UUID", "Invoice was already received."));
        _integrator.Given(Operation("GetInvoiceStatus")).RespondWith(Soap(
            $"""<GetInvoiceStatusResponse xmlns="{Ns}"><GetInvoiceStatusResult><Uuid>u</Uuid><EnvelopeId>ENV-9001</EnvelopeId><Status>QUEUED</Status></GetInvoiceStatusResult></GetInvoiceStatusResponse>"""));
        await using var api = CreateApi();

        using var response = await api.CreateClient().PostAsync("/api/orders/1003/invoice", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var invoice = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("ENV-9001", invoice.GetProperty("envelopeId").GetString());
        // WireMock logs the delayed request only after its late answer has been written.
        await Task.Delay(TimeSpan.FromSeconds(2.5), TestContext.Current.CancellationToken);
        Assert.Equal(2, Requests("SendInvoice"));
        Assert.Equal(1, Requests("GetInvoiceStatus"));
    }

    [Fact]
    public async Task Maintenance_503_is_not_retried()
    {
        _integrator.Given(Operation("SendInvoice")).RespondWith(Response.Create().WithStatusCode(503).WithHeader("Retry-After", "600"));
        await using var api = CreateApi();

        using var response = await api.CreateClient().PostAsync("/api/orders/1003/invoice", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(1, Requests("SendInvoice"));
    }
}
