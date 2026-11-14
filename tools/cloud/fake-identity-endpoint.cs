// Local stand-in for App Service's token endpoint (IDENTITY_ENDPOINT), only to see the protocol.
// It returns a fake token; nothing here talks to Microsoft Entra ID.
// Run: dotnet run tools/cloud/fake-identity-endpoint.cs
#:sdk Microsoft.NET.Sdk.Web
#:property PublishAot=false

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:5051");
builder.Logging.SetMinimumLevel(LogLevel.Warning);
WebApplication app = builder.Build();

const string ExpectedHeader = "local-test-header";

app.MapGet("/msi/token", (HttpRequest request) =>
{
    Console.WriteLine($"GET {request.Path}{request.QueryString}");
    Console.WriteLine($"  X-IDENTITY-HEADER present: {request.Headers.ContainsKey("X-IDENTITY-HEADER")}");
    if (request.Headers["X-IDENTITY-HEADER"] != ExpectedHeader)
    {
        return Results.Unauthorized();
    }
    long expiresOn = DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds();
    return Results.Json(new Dictionary<string, string>
    {
        ["access_token"] = "fake-token-for-local-test",
        ["expires_on"] = expiresOn.ToString(),
        ["resource"] = request.Query["resource"].ToString(),
        ["token_type"] = "Bearer",
        ["client_id"] = request.Query["client_id"].ToString(),
    });
});

app.Run();
