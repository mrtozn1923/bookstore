using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bookstore.E2E.Tests;

public sealed class CatalogTests(BrowserFixture fixture) : IAsyncLifetime
{
    private IBrowserContext context = null!;
    private IPage page = null!;

    public async ValueTask InitializeAsync()
    {
        context = await fixture.Browser.NewContextAsync(new BrowserNewContextOptions { BaseURL = fixture.BaseUrl });
        page = await context.NewPageAsync();
    }

    [Fact]
    public async Task Search_finds_a_book_with_a_lowercase_term()
    {
        await page.GotoAsync("/");

        await page.GetByLabel("Search books").FillAsync("dune");
        await page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync();

        ILocator results = page.GetByRole(AriaRole.List, new() { Name = "Search results" }).GetByRole(AriaRole.Listitem);
        await Expect(results).ToHaveCountAsync(1);
        await Expect(results.GetByRole(AriaRole.Heading)).ToHaveTextAsync("Dune");
    }

    public async ValueTask DisposeAsync() => await context.DisposeAsync();
}
