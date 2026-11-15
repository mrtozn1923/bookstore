using Bookstore.Admin.Components;
using Bookstore.Admin.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

string apiBaseUrl = builder.Configuration["BookstoreApi:BaseUrl"]
    ?? throw new InvalidOperationException("BookstoreApi:BaseUrl is missing (see appsettings.json).");
builder.Services.AddHttpClient<BookstoreApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl));

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
