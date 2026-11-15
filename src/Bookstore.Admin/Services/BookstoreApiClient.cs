using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using Bookstore.Admin.Client.Contracts;
using Bookstore.Admin.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.WebUtilities;

namespace Bookstore.Admin.Services;

public enum ApiWriteResult { Saved, Rejected, Forbidden, NotFound }

// Typed HttpClient for Bookstore.Api; the base address comes from configuration (BookstoreApi:BaseUrl).
// A typed client is created in the caller's DI scope (the circuit, or the request in Static SSR), so the
// AuthenticationStateProvider here is the current user's. Message handlers live in a scope of their own.
public sealed class BookstoreApiClient(HttpClient http, AuthenticationStateProvider authState, PanelTokenIssuer tokens)
{
    public async Task<PagedResult<BookSummary>> GetBooksAsync(
        string? search, string? category, int page, int pageSize, CancellationToken ct = default)
    {
        string uri = QueryHelpers.AddQueryString("api/books", new Dictionary<string, string?>
        {
            ["search"] = search,
            ["category"] = category,
            ["page"] = page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture)
        });
        return await GetOrNullAsync<PagedResult<BookSummary>>(uri, ct) ?? new PagedResult<BookSummary>([], 0, page, pageSize);
    }

    public async Task<IReadOnlyList<CategoryView>> GetCategoriesAsync(CancellationToken ct = default) =>
        await GetOrNullAsync<List<CategoryView>>("api/categories", ct) ?? [];

    public Task<BookDetails?> GetBookAsync(string isbn, CancellationToken ct = default) =>
        GetOrNullAsync<BookDetails>($"api/books/{Uri.EscapeDataString(isbn)}", ct);

    public Task<OrderView?> GetOrderAsync(int orderId, CancellationToken ct = default) =>
        GetOrNullAsync<OrderView>($"api/orders/{orderId}", ct);

    public async Task<IReadOnlyList<StockView>> GetStockAsync(CancellationToken ct = default) =>
        await GetOrNullAsync<List<StockView>>("api/inventory", ct) ?? [];

    // False when the API rejects the value (400), for example a count below the reserved quantity.
    public async Task<bool> SetQuantityOnHandAsync(string isbn, int quantityOnHand, CancellationToken ct = default) =>
        await WriteAsync(HttpMethod.Put, $"api/inventory/{Uri.EscapeDataString(isbn)}", new SetStockRequest(quantityOnHand), ct)
            == ApiWriteResult.Saved;

    public Task<ApiWriteResult> SetPriceAsync(string isbn, decimal price, CancellationToken ct = default) =>
        WriteAsync(HttpMethod.Put, $"api/books/{Uri.EscapeDataString(isbn)}/price", new SetPriceRequest(price), ct);

    // Null when the author was created; otherwise the API's validation errors by key, for example "Books[1].Isbn".
    public async Task<IDictionary<string, string[]>?> CreateAuthorAsync(NewAuthorRequest request, CancellationToken ct = default)
    {
        using HttpResponseMessage response = await SendAsync(HttpMethod.Post, "api/authors", JsonContent.Create(request), ct);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            HttpValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(ct);
            return problem?.Errors ?? new Dictionary<string, string[]>();
        }

        response.EnsureSuccessStatusCode();
        return null;
    }

    private async Task<ApiWriteResult> WriteAsync<T>(HttpMethod method, string uri, T body, CancellationToken ct)
    {
        using HttpResponseMessage response = await SendAsync(method, uri, JsonContent.Create(body), ct);
        switch (response.StatusCode)
        {
            case HttpStatusCode.BadRequest:
                return ApiWriteResult.Rejected;
            case HttpStatusCode.Forbidden:
                return ApiWriteResult.Forbidden;
            case HttpStatusCode.NotFound:
                return ApiWriteResult.NotFound;
            default:
                response.EnsureSuccessStatusCode();
                return ApiWriteResult.Saved;
        }
    }

    private async Task<T?> GetOrNullAsync<T>(string uri, CancellationToken ct) where T : class
    {
        using HttpResponseMessage response = await SendAsync(HttpMethod.Get, uri, null, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(ct);
    }

    // Every call carries a fresh token for the signed-in user; the API decides what that user may do.
    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string uri, HttpContent? content, CancellationToken ct)
    {
        // Read on every call and never cached, so a refreshed user is picked up; BL0013 (.NET 11) doesn't apply here.
#pragma warning disable BL0013
        AuthenticationState state = await authState.GetAuthenticationStateAsync();
#pragma warning restore BL0013
        using HttpRequestMessage request = new(method, uri) { Content = content };
        if (state.User.Identity?.IsAuthenticated == true)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.CreateToken(state.User));
        }

        return await http.SendAsync(request, ct);
    }
}
