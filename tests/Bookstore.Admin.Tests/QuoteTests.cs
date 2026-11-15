using Bookstore.Admin.Client.Catalog;
using Bookstore.Admin.Client.Contracts;
using Bookstore.Admin.Client.Ordering;
using Bookstore.Admin.Client.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.Admin.Tests;

public sealed class QuoteTests : BunitContext
{
    private static readonly BookSummary Dune = new("9780441172719", "Dune", "Frank Herbert", "Science Fiction", 489.00m, 1965);

    [Fact]
    public void Twenty_copies_get_the_institutional_discount_and_every_change_is_announced()
    {
        QuoteState quote = new();
        int changes = 0;
        quote.Changed += () => changes++;

        quote.Add(Dune, 19);
        Assert.Equal(0m, quote.Discount);

        quote.Add(Dune, 1);
        Assert.Equal(20, quote.Lines.Single().Quantity);
        Assert.Equal(978.00m, quote.Discount);
        Assert.Equal(8802.00m, quote.Total);
        Assert.Equal(2, changes);
    }

    [Fact]
    public void The_picker_adds_a_search_result_to_the_shared_quote()
    {
        // A fake catalog: the component only knows IBookCatalog, not EF Core or HTTP.
        Services.AddSingleton<IBookCatalog>(new FakeCatalog([Dune]));
        Services.AddScoped<QuoteState>();
        Services.AddCascadingValue(_ => new PanelOptions(Currency: "TRY", AmountFormat: "N2"));
        // BookPicker reads RendererInfo (the Search button is disabled until the component is interactive).
        Renderer.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        IRenderedComponent<BookPicker> picker = Render<BookPicker>();

        picker.Find("input[placeholder]").Change("Dune");
        picker.Find("input[type=number]").Change("3");
        picker.Find("button.btn-secondary").Click();
        picker.WaitForElement("li").QuerySelector("button")!.Click();

        QuoteLine line = Services.GetRequiredService<QuoteState>().Lines.Single();
        Assert.Equal(("Dune", 3), (line.Title, line.Quantity));
    }

    private sealed class FakeCatalog(IReadOnlyList<BookSummary> books) : IBookCatalog
    {
        public Task<PagedResult<BookSummary>> SearchAsync(string? search, int page, int pageSize, CancellationToken ct = default) =>
            Task.FromResult(new PagedResult<BookSummary>(
                [.. books.Where(b => search is null || b.Title.Contains(search))], books.Count, page, pageSize));
    }
}
