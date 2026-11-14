using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bookstore.Catalog;

namespace Bookstore.Api.Tests;

public sealed class CoverTests(BookstoreApiFactory factory)
{
    private const string Dune = "9780441172719";

    // The first bytes of a PNG file are enough for the signature check.
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];

    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Store_staff_uploads_a_png_cover_and_anyone_can_read_it()
    {
        HttpResponseMessage response = await factory.CreateClientFor(BookstoreApiFactory.Grace)
            .PutAsync($"/api/books/{Dune}/cover", CoverForm(PngBytes, "image/png"), ct);

        CoverUploaded? uploaded = await response.Content.ReadFromJsonAsync<CoverUploaded>(ct);
        Assert.Equal(new CoverUploaded(Dune, $"{Dune}.png", "image/png", PngBytes.Length), uploaded);

        HttpResponseMessage cover = await factory.CreateClient().GetAsync($"/api/books/{Dune}/cover", ct);
        Assert.Equal("image/png", cover.Content.Headers.ContentType?.MediaType);
        Assert.Equal(PngBytes, await cover.Content.ReadAsByteArrayAsync(ct));
    }

    [Fact]
    public async Task Customers_cannot_upload_covers()
    {
        HttpResponseMessage response = await factory.CreateClientFor(BookstoreApiFactory.Ada)
            .PutAsync($"/api/books/{Dune}/cover", CoverForm(PngBytes, "image/png"), ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_text_file_named_png_is_rejected_with_415()
    {
        HttpResponseMessage response = await factory.CreateClientFor(BookstoreApiFactory.Grace)
            .PutAsync($"/api/books/{Dune}/cover", CoverForm("not really an image"u8.ToArray(), "image/png"), ct);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    private static MultipartFormDataContent CoverForm(byte[] bytes, string contentType)
    {
        ByteArrayContent file = new(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { file, "file", "cover.png" } };
    }
}
