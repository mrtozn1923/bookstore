using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Bookstore.Api.Auth;
using Bookstore.Api.Dtos;
using Xunit;

namespace Bookstore.Api.IntegrationTests;

public class BooksApiTests : IClassFixture<BookstoreApiFactory>
{
    private readonly HttpClient _client;

    public BooksApiTests(BookstoreApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsSeededBooks()
    {
        var response = await _client.GetAsync("/api/books");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<BookDto>>();
        Assert.NotNull(page);
        Assert.Contains(page.Items, b => b.Title == "Dune");
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsProblemDetails()
    {
        var response = await _client.GetAsync("/api/books/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Create_ValidBook_ReturnsCreatedWithLocation()
    {
        await AuthenticateAsAdaAsync();

        var response = await _client.PostAsJsonAsync("/api/books", new { title = "Middlemarch", price = 390, categoryId = 2 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        // does the address in the Location header really work?
        var created = await _client.GetFromJsonAsync<BookDto>(response.Headers.Location);
        Assert.Equal("Middlemarch", created?.Title);
    }

    [Fact]
    public async Task Create_ZeroPrice_ReturnsValidationProblem()
    {
        await AuthenticateAsAdaAsync();

        var response = await _client.PostAsJsonAsync("/api/books", new { title = "Persuasion", price = 0, categoryId = 2 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("Price"));
    }

    // POST api/books needs a token: log in through the real login endpoint
    private async Task AuthenticateAsAdaAsync()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email = "ada@example.com", password = "Pa55word!" });
        login.EnsureSuccessStatusCode();
        var token = await login.Content.ReadFromJsonAsync<TokenResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
    }
}
