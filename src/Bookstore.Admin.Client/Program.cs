using Bookstore.Admin.Client.Shared;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddCascadingValue(_ => new PanelOptions(Currency: "TRY", AmountFormat: "N2"));

await builder.Build().RunAsync();
