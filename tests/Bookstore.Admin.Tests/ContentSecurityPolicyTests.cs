using Bookstore.Admin.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.Admin.Tests;

public sealed class ContentSecurityPolicyTests
{
    private static async Task<HttpContext> SendAsync()
    {
        ApplicationBuilder app = new(new ServiceCollection().BuildServiceProvider());
        app.UseContentSecurityPolicy();
        app.Run(_ => Task.CompletedTask);
        DefaultHttpContext context = new();
        await app.Build()(context);
        return context;
    }

    [Fact]
    public async Task The_header_allows_the_import_map_by_the_nonce_of_this_response()
    {
        HttpContext context = await SendAsync();

        string nonce = Assert.IsType<string>(context.Items[ContentSecurityPolicy.NonceKey]);
        Assert.Contains($"'nonce-{nonce}'", context.Response.Headers.ContentSecurityPolicy.ToString());
        Assert.Contains("'wasm-unsafe-eval'", context.Response.Headers.ContentSecurityPolicy.ToString());
    }

    [Fact]
    public async Task Every_response_gets_a_new_nonce()
    {
        HttpContext first = await SendAsync();
        HttpContext second = await SendAsync();

        Assert.NotEqual(first.Items[ContentSecurityPolicy.NonceKey], second.Items[ContentSecurityPolicy.NonceKey]);
    }
}
