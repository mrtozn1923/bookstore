WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

WebApplication app = builder.Build();

// In the published image wwwroot holds Angular's build output; locally the folder doesn't exist
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapDefaultEndpoints();
app.MapReverseProxy();
app.MapFallbackToFile("index.html");

app.Run();
