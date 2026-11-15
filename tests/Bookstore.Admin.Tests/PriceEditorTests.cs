using System.Net;
using Bookstore.Admin.Client.Auth;
using Bookstore.Admin.Components.Books;
using Bookstore.Admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.Admin.Tests;

public sealed class PriceEditorTests : BunitContext
{
    private const string Emma = "9780141439587";

    private void SignIn(string email, string role)
    {
        // The panel's real policies instead of bUnit's AddAuthorization() fake, so the test checks the role mapping too.
        // bUnit registers a placeholder IAuthorizationService that throws; the registration below replaces it.
        FixedAuthenticationStateProvider authState = new(TestStaff.User(email, role));
        Services.AddAuthorizationCore(options => options.AddPanelPolicies());
        Services.AddSingleton<IAuthorizationService, DefaultAuthorizationService>();
        Services.AddSingleton<AuthenticationStateProvider>(authState);
        Services.AddCascadingAuthenticationState();
        Services.AddSingleton(new BookstoreApiClient(
            new HttpClient(new StatusHandler(HttpStatusCode.NoContent)) { BaseAddress = new Uri("http://localhost:5080/") },
            authState, TestStaff.Tokens()));
    }

    [Fact]
    public void Store_staff_see_the_price_form()
    {
        SignIn("grace@example.com", PanelPolicies.StoreStaff);

        IRenderedComponent<PriceEditor> editor = Render<PriceEditor>(p => p.Add(x => x.Isbn, Emma).Add(x => x.CurrentPrice, 249m));

        Assert.Equal("249", editor.Find("form input").GetAttribute("value"));
    }

    [Fact]
    public void Warehouse_staff_see_a_note_instead_of_the_form()
    {
        SignIn("linus@example.com", PanelPolicies.WarehouseStaff);

        IRenderedComponent<PriceEditor> editor = Render<PriceEditor>(p => p.Add(x => x.Isbn, Emma).Add(x => x.CurrentPrice, 249m));

        Assert.Empty(editor.FindAll("form"));
        Assert.Equal("Only store staff can change prices.", editor.Find("p").TextContent);
    }

    private sealed class StatusHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status));
    }
}
