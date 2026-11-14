using System.Text;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

// The "before" side of the migration: Bookstore's old gateway, written with Ocelot.
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

IConfigurationSection auth = builder.Configuration.GetSection("Auth");
builder.Services.AddAuthentication()
    .AddJwtBearer("Bearer", options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = auth["Issuer"],
            ValidAudience = auth["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(auth["SigningKey"]!)),
        };
    });
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();
await app.UseOcelot();
await app.RunAsync();
