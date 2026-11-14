using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy;
using Yarp.ReverseProxy.Model;

// The "after" side: the routes of samples/Bookstore.Gateway.Ocelot/ocelot.json, translated to YARP.
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile(builder.Configuration["ProxyConfig"] ?? "yarp.json", optional: false, reloadOnChange: true);

IConfigurationSection auth = builder.Configuration.GetSection("Auth");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = auth["Issuer"],
            ValidAudience = auth["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(auth["SigningKey"]!)),
        };
    });
builder.Services.AddAuthorization();

// Ocelot's RateLimitOptions counted per X-Client-Id header; here the partition is the validated user.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("per-user", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue("sub") ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromSeconds(10), QueueLimit = 0 }));
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapReverseProxy();

if (app.Environment.IsDevelopment())
{
    // What YARP actually made of the configuration: useful when reading an old config file.
    app.MapGet("/debug/routes", (IProxyStateLookup proxyState) => proxyState.GetRoutes().Select(route => new
    {
        route.Config.RouteId,
        route.Config.Match.Path,
        route.Config.ClusterId,
        route.Config.Order,
        ActivityTimeout = route.Cluster?.Model.Config.HttpRequest?.ActivityTimeout?.ToString("c", CultureInfo.InvariantCulture),
        route.Cluster?.Model.Config.LoadBalancingPolicy,
    }));
}

app.Run();
