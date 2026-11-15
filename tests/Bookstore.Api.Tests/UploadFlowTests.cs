using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bookstore.Api.Uploads;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Bookstore.Api.Tests;

public sealed class UploadFlowTests(BookstoreFactory factory, ITestOutputHelper output) : IClassFixture<BookstoreFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private readonly HttpClient storage = new();
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Sample_chapter_goes_through_quarantine_and_is_downloaded_as_attachment()
    {
        HttpClient api = factory.ClientFor("pub-7");
        byte[] pdf = TestFiles.Pdf();
        CreateUploadResponse created = await CreateAsync(api, UploadPurpose.SampleChapter, "Dune sample.pdf", pdf.Length);
        output.WriteLine($"upload url: {Redact(created.UploadUrl)}");

        HttpResponseMessage put = await PutBlobAsync(created.UploadUrl, pdf);
        output.WriteLine($"PUT with SAS            -> {(int)put.StatusCode}");

        HttpResponseMessage early = await api.GetAsync($"/uploads/{created.Id}/download", Ct);
        output.WriteLine($"download before scan    -> {(int)early.StatusCode}");

        UploadStatusResponse status = await CompleteAsync(api, created.Id);
        output.WriteLine($"status                  -> {status.Status}");

        HttpResponseMessage other = await factory.ClientFor("pub-9").GetAsync($"/uploads/{created.Id}/download", Ct);
        output.WriteLine($"download, other publisher -> {(int)other.StatusCode}");

        HttpResponseMessage redirect = await api.GetAsync($"/uploads/{created.Id}/download", Ct);
        HttpResponseMessage file = await storage.GetAsync(redirect.Headers.Location, Ct);
        output.WriteLine($"download                -> {(int)redirect.StatusCode} then {(int)file.StatusCode}");
        output.WriteLine($"Content-Type: {file.Content.Headers.ContentType}");
        output.WriteLine($"Content-Disposition: {file.Content.Headers.ContentDisposition}");

        Assert.Equal(HttpStatusCode.Created, put.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, early.StatusCode);
        Assert.Equal(UploadStatus.Approved, status.Status);
        Assert.Equal(HttpStatusCode.NotFound, other.StatusCode);
        Assert.Equal("attachment", file.Content.Headers.ContentDisposition?.DispositionType);
    }

    [Fact]
    public async Task Upload_sas_is_limited_to_creating_one_blob()
    {
        HttpClient api = factory.ClientFor("pub-7");
        CreateUploadResponse created = await CreateAsync(api, UploadPurpose.SampleChapter, "dune.pdf", 100);
        Uri sibling = new(created.UploadUrl.ToString().Replace(created.Id.ToString("N"), Guid.NewGuid().ToString("N")));

        HttpResponseMessage first = await PutBlobAsync(created.UploadUrl, TestFiles.Pdf());
        HttpResponseMessage again = await PutBlobAsync(created.UploadUrl, TestFiles.Svg());
        HttpResponseMessage elsewhere = await PutBlobAsync(sibling, TestFiles.Pdf());
        HttpResponseMessage read = await storage.GetAsync(created.UploadUrl, Ct);

        output.WriteLine($"PUT own blob            -> {(int)first.StatusCode}");
        output.WriteLine($"PUT own blob again      -> {(int)again.StatusCode} {await ErrorCodeAsync(again)}");
        output.WriteLine($"PUT another blob name   -> {(int)elsewhere.StatusCode} {await ErrorCodeAsync(elsewhere)}");
        output.WriteLine($"GET with upload SAS     -> {(int)read.StatusCode} {await ErrorCodeAsync(read)}");
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, elsewhere.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, read.StatusCode);
    }

    [Fact]
    public async Task Each_check_gets_its_own_verdict()
    {
        HttpClient api = factory.ClientFor("pub-7");
        (string Name, UploadPurpose Purpose, byte[] Content)[] cases =
        [
            ("dune-cover.jpg (polyglot)", UploadPurpose.Cover, TestFiles.Polyglot()),
            ("dune-cover.jpg (svg)", UploadPurpose.Cover, TestFiles.Svg()),
            ("dune-sample.epub", UploadPurpose.SampleChapter, TestFiles.Epub(("OEBPS/ch1.xhtml", TestFiles.Chapter()))),
            ("dune-sample.epub (eicar)", UploadPurpose.SampleChapter, TestFiles.Epub(("OEBPS/ch1.xhtml", TestFiles.Chapter()), ("OEBPS/fonts/serif.ttf", TestFiles.Eicar()))),
            ("dune-sample.pdf (25 MB)", UploadPurpose.SampleChapter, [.. TestFiles.Pdf(), .. new byte[25 * 1024 * 1024]]),
        ];

        foreach ((string name, UploadPurpose purpose, byte[] content) in cases)
        {
            // Declares a small size: the SAS does not care, the processor does
            CreateUploadResponse created = await CreateAsync(api, purpose, name, 1024);
            await PutBlobAsync(created.UploadUrl, content);
            UploadStatusResponse status = await CompleteAsync(api, created.Id);
            output.WriteLine($"{name,-26} {status.Status,-9} {status.Reason}");
        }
    }

    [Fact]
    public async Task Silent_scanner_leaves_the_file_in_quarantine()
    {
        // Our own listener that accepts the connection and never answers
        using TcpListener silent = new(IPAddress.Loopback, 0);
        silent.Start();
        Task<TcpClient> accepted = silent.AcceptTcpClientAsync(Ct).AsTask();
        using WebApplicationFactory<Program> broken = factory.WithWebHostBuilder(b => b
            .UseSetting("ClamAv:Port", ((IPEndPoint)silent.LocalEndpoint).Port.ToString())
            .UseSetting("ClamAv:Timeout", "00:00:02"));

        HttpClient api = broken.CreateClient();
        api.DefaultRequestHeaders.Add(TestAuthHandler.Header, "pub-7");
        CreateUploadResponse created = await CreateAsync(api, UploadPurpose.SampleChapter, "dune.pdf", 100);
        await PutBlobAsync(created.UploadUrl, TestFiles.Pdf());
        UploadStatusResponse status = await CompleteAsync(api, created.Id);
        HttpResponseMessage download = await api.GetAsync($"/uploads/{created.Id}/download", Ct);

        output.WriteLine($"status {status.Status} ({status.Reason}), download -> {(int)download.StatusCode}");
        Assert.Equal(UploadStatus.ScanFailed, status.Status);
        Assert.Equal(HttpStatusCode.Conflict, download.StatusCode);
        (await accepted).Dispose();
    }

    private async Task<CreateUploadResponse> CreateAsync(HttpClient api, UploadPurpose purpose, string fileName, long length)
    {
        HttpResponseMessage response = await api.PostAsJsonAsync("/uploads", new CreateUploadRequest("9780441013593", purpose, fileName, length), Json, Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateUploadResponse>(Json, Ct))!;
    }

    private async Task<UploadStatusResponse> CompleteAsync(HttpClient api, Guid id)
    {
        (await api.PostAsync($"/uploads/{id}/complete", null, Ct)).EnsureSuccessStatusCode();
        for (int i = 0; i < 100; i++)
        {
            UploadStatusResponse status = (await api.GetFromJsonAsync<UploadStatusResponse>($"/uploads/{id}", Json, Ct))!;
            if (status.Status is not UploadStatus.Processing)
            {
                return status;
            }

            await Task.Delay(200, Ct);
        }

        throw new TimeoutException($"Upload {id} is still processing.");
    }

    private Task<HttpResponseMessage> PutBlobAsync(Uri sasUrl, byte[] content)
    {
        HttpRequestMessage request = new(HttpMethod.Put, sasUrl) { Content = new ByteArrayContent(content) };
        request.Headers.Add("x-ms-blob-type", "BlockBlob");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");  // the client can claim anything
        return storage.SendAsync(request, Ct);
    }

    private static async Task<string> ErrorCodeAsync(HttpResponseMessage response) =>
        response.IsSuccessStatusCode ? "" : response.Headers.TryGetValues("x-ms-error-code", out var code) ? code.First() : await response.Content.ReadAsStringAsync();

    private static string Redact(Uri url) => System.Text.RegularExpressions.Regex.Replace(url.ToString(), "sig=[^&]+", "sig=<signature>");
}
