using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace Bookstore.Api.Tests;

public class ForwardedHeadersTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private record WhoAmI(string ClientIp, string Scheme);

    private Task<HttpContext> SendFrom(string peer) =>
        factory.Server.SendAsync(c =>
        {
            c.Request.Method = "GET";
            c.Request.Scheme = "http";
            c.Request.Host = new HostString("bookstore.example");
            c.Request.Path = "/api/whoami";
            c.Connection.RemoteIpAddress = IPAddress.Parse(peer);
            c.Request.Headers["CF-Connecting-IP"] = "198.51.100.7";
            c.Request.Headers["X-Forwarded-Proto"] = "https";
        });

    [Fact]
    public async Task A_Cloudflare_edge_is_trusted()
    {
        var context = await SendFrom("173.245.48.10");

        var who = await JsonSerializer.DeserializeAsync<WhoAmI>(context.Response.Body, JsonSerializerOptions.Web);
        Assert.Equal("198.51.100.7", who!.ClientIp);
        Assert.Equal("https", who.Scheme);
    }

    [Fact]
    public async Task Anyone_else_is_not_trusted()
    {
        var context = await SendFrom("203.0.113.50");

        // the spoofed headers are ignored: the request is still plain HTTP, so the origin redirects it
        Assert.Equal(StatusCodes.Status307TemporaryRedirect, context.Response.StatusCode);
        Assert.Equal("https://bookstore.example/api/whoami", context.Response.Headers.Location.ToString());
    }
}
