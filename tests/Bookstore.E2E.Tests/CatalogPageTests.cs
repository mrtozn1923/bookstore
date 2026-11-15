using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

namespace Bookstore.E2E.Tests;

// PageTest her test için yeni bir BrowserContext ve Page açar; tarayıcı BROWSER ortam değişkeninden gelir (varsayılan chromium)
public sealed class CatalogPageTests : PageTest
{
    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = Environment.GetEnvironmentVariable("BOOKSTORE_WEB_URL") ?? "http://localhost:4200",
        Locale = "en-US",
        TimezoneId = "Europe/Istanbul",
    };

    [Fact]
    public async Task Out_of_stock_book_cannot_be_added()
    {
        await Page.GotoAsync("/");

        ILocator hobbit = Page.GetByRole(AriaRole.Listitem).Filter(new() { HasText = "The Hobbit" });
        await Expect(hobbit.GetByText("Out of stock")).ToBeVisibleAsync();
        await Expect(hobbit.GetByRole(AriaRole.Button, new() { Name = "Add to cart" })).ToBeDisabledAsync();
    }
}
