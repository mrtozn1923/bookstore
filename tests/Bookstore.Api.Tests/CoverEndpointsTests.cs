using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.Api.Tests;

public sealed class CoverEndpointsTests(BookstoreApiFactory factory)
{
    private const string Dune = "9780441172719";
    // A valid 1x1 PNG
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private readonly HttpClient api = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    private readonly HttpClient browser = new(AzuriteOAuth.CreateHandler());

    [Fact]
    public async Task Uploaded_cover_is_served_through_a_read_only_user_delegation_sas()
    {
        Assert.SkipWhen(AzuriteOAuth.ServiceUri is null, "AZURITE_OAUTH_BLOB_URI is not set");
        CancellationToken ct = TestContext.Current.CancellationToken;

        HttpResponseMessage upload = await api.PutAsync($"/api/books/{Dune}/cover", Form(Png, "dune.png"), ct);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);

        HttpResponseMessage redirect = await api.GetAsync($"/api/books/{Dune}/cover", ct);
        Assert.Equal(HttpStatusCode.Redirect, redirect.StatusCode);
        Uri sasUrl = redirect.Headers.Location!;
        TestContext.Current.TestOutputHelper?.WriteLine($"Location: {sasUrl}");
        Assert.Matches($@"/covers/{Dune}-[0-9a-f]{{32}}\.png$", sasUrl.AbsolutePath);
        Assert.Contains("sp=r&", sasUrl.Query);
        Assert.Contains("spr=https", sasUrl.Query);

        HttpResponseMessage image = await browser.GetAsync(sasUrl, ct);
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal("image/png", image.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Png, await image.Content.ReadAsByteArrayAsync(ct));

        // The signature covers the blob name and the permissions: changing either one is rejected
        HttpResponseMessage otherBlob = await browser.GetAsync(sasUrl.ToString().Replace($"{Dune}-", $"{Dune}-0"), ct);
        HttpResponseMessage morePermissions = await browser.GetAsync(sasUrl.ToString().Replace("sp=r&", "sp=rw&"), ct);
        Assert.Equal(HttpStatusCode.Forbidden, otherBlob.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, morePermissions.StatusCode);
    }

    [Fact]
    public async Task A_new_cover_replaces_the_old_blob()
    {
        Assert.SkipWhen(AzuriteOAuth.ServiceUri is null, "AZURITE_OAUTH_BLOB_URI is not set");
        CancellationToken ct = TestContext.Current.CancellationToken;
        BlobContainerClient covers = factory.Services.GetRequiredService<BlobContainerClient>();
        const string emma = "9780141439587";

        CoverResult first = (await (await api.PutAsync($"/api/books/{emma}/cover", Form(Png, "a.png"), ct)).Content.ReadFromJsonAsync<CoverResult>(ct))!;
        CoverResult second = (await (await api.PutAsync($"/api/books/{emma}/cover", Form(Png, "a.png"), ct)).Content.ReadFromJsonAsync<CoverResult>(ct))!;

        Assert.NotEqual(first.BlobName, second.BlobName);
        Assert.False(await covers.GetBlobClient(first.BlobName).ExistsAsync(ct));
        Assert.True(await covers.GetBlobClient(second.BlobName).ExistsAsync(ct));
    }

    [Fact]
    public async Task A_text_file_named_jpg_is_rejected_with_415()
    {
        HttpResponseMessage response = await api.PutAsync(
            $"/api/books/{Dune}/cover", Form("<html>not an image</html>"u8.ToArray(), "cover.jpg", "image/jpeg"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task A_cover_over_2_mb_is_rejected_before_it_reaches_storage()
    {
        byte[] large = new byte[3 * 1024 * 1024];
        Png.CopyTo(large, 0);

        HttpResponseMessage response = await api.PutAsync($"/api/books/{Dune}/cover", Form(large, "large.png"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_book_returns_404()
    {
        HttpResponseMessage response = await api.GetAsync("/api/books/9780000000000/cover", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static MultipartFormDataContent Form(byte[] bytes, string fileName, string contentType = "image/png")
    {
        ByteArrayContent file = new(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { file, "file", fileName } };
    }

    private sealed record CoverResult(string Isbn, string BlobName, string ContentType, long Size);
}
