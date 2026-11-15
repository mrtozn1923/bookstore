using System.Net;
using System.Net.Http.Json;
using Bookstore.Admin.Client.Contracts;
using Bookstore.Admin.Components.Pages.Authors;
using Bookstore.Admin.Services;
using Bookstore.Admin.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.Admin.Tests;

public sealed class NewAuthorPageTests : BunitContext
{
    private readonly List<string> requests = [];

    public NewAuthorPageTests()
    {
        // A fake Bookstore.Api: categories for the select list, a 400 for an ISBN that already exists.
        StubHandler handler = new(request =>
        {
            requests.Add($"{request.Method} {request.RequestUri!.AbsolutePath} {request.Headers.Authorization?.Scheme}".TrimEnd());
            return request.RequestUri.AbsolutePath switch
            {
                "/api/categories" => Json(HttpStatusCode.OK, new[] { new CategoryView("Classics", "classics") }),
                "/api/authors" => Json(HttpStatusCode.BadRequest, new
                {
                    title = "One or more validation errors occurred.",
                    status = 400,
                    errors = new Dictionary<string, string[]> { ["Books[0].Isbn"] = ["A book with ISBN 9780441172719 already exists."] }
                }),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound)
            };
        });
        Services.AddSingleton(new BookstoreApiClient(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5080/") },
            new FixedAuthenticationStateProvider(TestStaff.User("grace@example.com", "StoreStaff")),
            TestStaff.Tokens()));
        Services.AddAdminValidation();
    }

    [Fact]
    public void An_empty_form_is_not_sent_to_the_api()
    {
        IRenderedComponent<NewAuthorPage> page = Render<NewAuthorPage>();

        page.Find("form").Submit();

        Assert.Equal(6, page.FindAll("div.validation-message").Count);
        Assert.DoesNotContain(requests, r => r.StartsWith("POST /api/authors"));
    }

    [Fact]
    public void An_api_error_appears_under_the_field_it_belongs_to()
    {
        IRenderedComponent<NewAuthorPage> page = Render<NewAuthorPage>();
        page.Find("label:contains('Name') input").Change("Frank Herbert Jr");
        page.Find("fieldset label:contains('ISBN') input").Change("9780441172719");
        page.Find("fieldset label:contains('Title') input").Change("Dune Again");
        page.Find("fieldset label:contains('Price') input").Change("300");
        page.Find("fieldset label:contains('Year') input").Change("1965");
        page.Find("fieldset select").Change("classics");

        page.Find("form").Submit();

        page.WaitForAssertion(() =>
        {
            Assert.Contains("POST /api/authors Bearer", requests);
            Assert.Equal("A book with ISBN 9780441172719 already exists.",
                page.Find("fieldset label:contains('ISBN') + div.validation-message").TextContent);
        });
    }

    private static HttpResponseMessage Json(HttpStatusCode status, object body) =>
        new(status) { Content = JsonContent.Create(body) };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
