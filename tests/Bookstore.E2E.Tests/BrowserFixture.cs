using Microsoft.Playwright;

[assembly: AssemblyFixture(typeof(Bookstore.E2E.Tests.BrowserFixture))]
[assembly: Trait("Category", "E2E")]

namespace Bookstore.E2E.Tests;

public sealed class BrowserFixture : IAsyncLifetime
{
    private IPlaywright? playwright;

    public IBrowser Browser { get; private set; } = null!;

    public string BaseUrl { get; } = Environment.GetEnvironmentVariable("BOOKSTORE_WEB_URL") ?? "http://localhost:4200";

    public async ValueTask InitializeAsync()
    {
        playwright = await Playwright.CreateAsync();
        Browser = await playwright.Chromium.LaunchAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Browser.DisposeAsync();
        playwright?.Dispose();
    }
}
