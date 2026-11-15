using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

MsSqlContainer sql = new MsSqlBuilder().Build();
Console.WriteLine(sql.GetType().Name);
Environment.CurrentDirectory = AppContext.BaseDirectory;
await using CustomWebApplicationFactory factory = new();
HttpResponseMessage response = await factory.CreateClient().GetAsync("/api/books");
Console.WriteLine($"GET /api/books -> {(int)response.StatusCode} {response.StatusCode}");

public class CustomWebApplicationFactory : WebApplicationFactory<Startup>
{
    protected override IWebHostBuilder CreateWebHostBuilder() =>
        WebHost.CreateDefaultBuilder().UseStartup<Startup>();

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseContentRoot(AppContext.BaseDirectory);

    protected override TestServer CreateServer(IWebHostBuilder builder) => base.CreateServer(builder);
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services) => services.AddRouting();

    public void Configure(IApplicationBuilder app) => app.UseRouting();
}
