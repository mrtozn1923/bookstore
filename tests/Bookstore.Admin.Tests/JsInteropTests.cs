using Bookstore.Admin.Components.Books;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bookstore.Admin.Tests;

public sealed class JsInteropTests : BunitContext
{
    private const string ModulePath = "./Components/Books/CopyIsbnButton.razor.js";

    public JsInteropTests() => Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

    [Theory]
    [InlineData(true, "Copied")]
    [InlineData(false, "Copy failed")]
    public void The_button_shows_what_the_clipboard_call_returned(bool copied, string expected)
    {
        // bUnit's JSInterop is strict: every call the component makes must be set up.
        BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
        module.Setup<bool>("copyText", "9780141439549").SetResult(copied);
        IRenderedComponent<CopyIsbnButton> button = Render<CopyIsbnButton>(p => p.Add(b => b.Isbn, "9780141439549"));

        button.Find("button").Click();

        Assert.Equal(expected, button.Find("button").TextContent.Trim());
        Assert.Single(module.Invocations["copyText"]);
    }

    [Fact]
    public void The_module_is_not_loaded_until_the_first_click()
    {
        JSInterop.SetupModule(ModulePath);

        Render<CopyIsbnButton>(p => p.Add(b => b.Isbn, "9780141439549"));

        Assert.Empty(JSInterop.Invocations);
    }

    [Fact]
    public void A_js_error_shows_the_copy_failure_instead_of_ending_the_circuit()
    {
        JSInterop.SetupModule(ModulePath).Setup<bool>("copyText", "9780141439549")
            .SetException(new JSException("Clipboard is not available"));
        IRenderedComponent<CopyIsbnButton> button = Render<CopyIsbnButton>(p => p.Add(b => b.Isbn, "9780141439549"));

        button.Find("button").Click();

        Assert.Equal("Copy failed", button.Find("button").TextContent.Trim());
    }
}
