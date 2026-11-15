using Bookstore.Admin.Client.Catalog;
using Bookstore.Admin.Client.Ordering;
using Bookstore.Admin.Client.Shared;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddCascadingValue(_ => new PanelOptions(Currency: "TRY", AmountFormat: "N2"));

// In the browser the panel itself is the API: same origin, the request carries the panel's cookies.
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<IBookCatalog, HttpBookCatalog>();
builder.Services.AddScoped<QuoteState>();

await builder.Build().RunAsync();
