using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace Bookstore.Api.Tests;

public class EdgeProtectionTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly byte[] WrongPassword = Encoding.UTF8.GetBytes("""{"email":"ada@example.com","password":"wrong"}""");

    private static Task<HttpContext> Login(TestServer server, string clientIp) =>
        server.SendAsync(c =>
        {
            c.Request.Method = "POST";
            c.Request.Scheme = "http";
            c.Request.Host = new HostString("bookstore.example");
            c.Request.Path = "/api/auth/login";
            c.Request.ContentType = "application/json";
            c.Request.ContentLength = WrongPassword.Length;
            c.Request.Body = new MemoryStream(WrongPassword);
            c.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestHasBody());
            c.Connection.RemoteIpAddress = IPAddress.Parse("173.245.48.10");  // every request arrives from the same Cloudflare edge
            c.Request.Headers["CF-Connecting-IP"] = clientIp;
            c.Request.Headers["X-Forwarded-Proto"] = "https";
        });

    [Fact]
    public async Task The_login_limit_is_per_visitor_not_per_edge()
    {
        var server = factory.Server;
        var attacker = new List<int>();
        for (var i = 0; i < 11; i++)
        {
            attacker.Add((await Login(server, "198.51.100.66")).Response.StatusCode);
        }

        var customer = (await Login(server, "198.51.100.7")).Response.StatusCode;

        Assert.Equal([.. Enumerable.Repeat(401, 10), 429], attacker);
        Assert.Equal(401, customer);  // wrong password, but not locked out by someone else's attempts
    }

    [Fact]
    public async Task Front_Door_id_is_enforced_when_configured()
    {
        var server = factory.WithWebHostBuilder(b => b.UseSetting("FrontDoor:Id", "8f4b1c2e-0000-4000-8000-000000000001")).Server;

        var withoutId = await server.SendAsync(c => c.Request.Path = "/api/books");
        var withId = await server.SendAsync(c =>
        {
            c.Request.Path = "/api/books";
            c.Request.Headers["X-Azure-FDID"] = "8f4b1c2e-0000-4000-8000-000000000001";
        });

        Assert.Equal(403, withoutId.Response.StatusCode);
        Assert.NotEqual(403, withId.Response.StatusCode);
    }
}

// tells TestServer that this hand-built request has a body
file sealed class RequestHasBody : IHttpRequestBodyDetectionFeature
{
    public bool CanHaveBody => true;
}
