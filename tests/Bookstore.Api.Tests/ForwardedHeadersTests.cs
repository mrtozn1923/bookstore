using System.Net;
using Bookstore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Tests;

public class ForwardedHeadersTests
{
    [Theory]
    [InlineData("10.0.0.5", "https")]          // App Service front end on a private address
    [InlineData("::ffff:10.0.0.5", "https")]   // same address as Kestrel sees it on a dual-mode socket
    [InlineData("203.0.113.9", "http")]        // anyone else: the header is ignored
    public async Task X_Forwarded_Proto_is_trusted_only_from_known_networks(string remoteIp, string expectedScheme)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Proxy:KnownNetworks"] = "10.0.0.0/8;172.16.0.0/12;192.168.0.0/16" })
            .Build();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddCloudHosting(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        IApplicationBuilder app = new ApplicationBuilder(provider);
        app.UseForwardedHeaders(provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value);
        app.Run(_ => Task.CompletedTask);
        RequestDelegate pipeline = app.Build();

        DefaultHttpContext context = new();
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        context.Request.Scheme = "http";
        context.Request.Headers["X-Forwarded-Proto"] = "https";
        context.Request.Headers["X-Forwarded-For"] = "198.51.100.20";

        await pipeline(context);

        Assert.Equal(expectedScheme, context.Request.Scheme);
    }
}
