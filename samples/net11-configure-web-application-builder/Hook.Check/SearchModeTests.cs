using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Hook.Check;

public sealed class OldStyleFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureAppConfiguration(config =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["Catalog:SearchMode"] = "full-text" }));
}

public sealed class NewHookFactory : WebApplicationFactory<Program>
{
    // .NET 11: WebApplication.CreateBuilder çağrısının içinde, Program.cs devam etmeden önce çalışır
    protected override void ConfigureWebApplicationBuilder(IHostApplicationBuilder builder) =>
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Catalog:SearchMode"] = "full-text" });
}

public sealed class SearchModeTests
{
    [Fact]
    public async Task ConfigureAppConfiguration_value_arrives_too_late()
    {
        await using OldStyleFactory factory = new();
        string mode = await factory.CreateClient().GetStringAsync("/search-mode", TestContext.Current.CancellationToken);
        Assert.Equal("full-text", mode);
    }

    [Fact]
    public async Task ConfigureWebApplicationBuilder_value_is_visible_before_Build()
    {
        await using NewHookFactory factory = new();
        string mode = await factory.CreateClient().GetStringAsync("/search-mode", TestContext.Current.CancellationToken);
        Assert.Equal("full-text", mode);
    }
}
