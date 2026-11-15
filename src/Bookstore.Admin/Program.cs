using Bookstore.Admin.Components;
using Bookstore.Admin.Client.Shared;
using Bookstore.Admin.Services;
using Bookstore.Admin.Validation;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

string apiBaseUrl = builder.Configuration["BookstoreApi:BaseUrl"]
    ?? throw new InvalidOperationException("BookstoreApi:BaseUrl is missing (see appsettings.json).");
builder.Services.AddHttpClient<BookstoreApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<StaffNoteStore>();
// A root-level cascading value reaches every component, whatever its render mode (the WebAssembly side registers its own).
builder.Services.AddCascadingValue(_ => new PanelOptions(Currency: "TRY", AmountFormat: "N2"));
// Source-generated validation metadata: nested objects and collections in [ValidatableType] form models.
builder.Services.AddAdminValidation();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Bookstore.Admin.Client._Imports).Assembly);

app.Run();
