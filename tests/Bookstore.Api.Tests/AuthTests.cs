using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Bookstore.Identity;
using Bookstore.Ordering;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Tests;

public sealed class AuthTests(BookstoreApiFactory factory)
{
    private const string Emma = "9780141439587";

    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Login_returns_a_bearer_token_that_identifies_the_user()
    {
        HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest(BookstoreApiFactory.Ada, "Bookstore-Demo-1"), ct);

        TokenResponse? token = await response.Content.ReadFromJsonAsync<TokenResponse>(ct);
        Assert.Equal("Bearer", token?.TokenType);
        Assert.Equal(900, token?.ExpiresIn);

        client.DefaultRequestHeaders.Authorization = new("Bearer", token!.AccessToken);
        CurrentUser? me = await client.GetFromJsonAsync<CurrentUser>("/api/auth/me", ct);
        Assert.Equal(new CurrentUser(BookstoreApiFactory.Ada, "Ada Lovelace", "Customer"), me);
    }

    [Fact]
    public async Task Wrong_password_returns_401_problem_details()
    {
        HttpResponseMessage response = await factory.CreateClient().PostAsJsonAsync(
            "/api/auth/login", new LoginRequest(BookstoreApiFactory.Ada, "wrong"), ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ct);
        Assert.Equal("Invalid email or password.", problem?.Detail);
    }

    [Fact]
    public async Task Orders_without_a_token_return_401_with_a_bearer_challenge()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/api/orders/1", ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.Single().Scheme);
    }

    [Fact]
    public async Task A_customer_sees_only_own_orders_while_staff_sees_all()
    {
        HttpClient ada = factory.CreateClientFor(BookstoreApiFactory.Ada);
        CartView? cart = await (await ada.PostAsync("/api/carts", null, ct)).Content.ReadFromJsonAsync<CartView>(ct);
        (await ada.PutAsJsonAsync($"/api/carts/{cart!.Id}/items/{Emma}", new SetQuantityRequest(1), ct)).EnsureSuccessStatusCode();
        HttpResponseMessage placed = await ada.PostAsJsonAsync(
            "/api/orders", new PlaceOrderRequest(cart.Id, "Alan Turing", "alan@example.com"), ct);
        string location = placed.Headers.Location!.AbsolutePath;

        HttpResponseMessage asAda = await ada.GetAsync(location, ct);
        HttpResponseMessage asGrace = await factory.CreateClientFor(BookstoreApiFactory.Grace).GetAsync(location, ct);

        // The order belongs to alan@example.com, not to Ada.
        Assert.Equal(HttpStatusCode.NotFound, asAda.StatusCode);
        Assert.Equal(HttpStatusCode.OK, asGrace.StatusCode);
    }

    [Fact]
    public async Task OpenApi_document_marks_orders_with_the_bearer_scheme()
    {
        JsonNode? document = await factory.CreateClient().GetFromJsonAsync<JsonNode>("/openapi/v1.json", ct);

        Assert.Equal("bearer", (string?)document?["components"]?["securitySchemes"]?["Bearer"]?["scheme"]);
        Assert.NotNull(document?["paths"]?["/api/orders"]?["post"]?["security"]);
        Assert.Null(document?["paths"]?["/api/books"]?["get"]?["security"]);
    }
}
