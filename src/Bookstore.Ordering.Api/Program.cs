using System.Security.Claims;
using System.Text;
using Bookstore.Ordering;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Defense in depth: the gateway already validated the token, Ordering validates it again.
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
            NameClaimType = "name",
            RoleClaimType = "role",
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

List<Order> orders =
[
    new(1001, "ada", "ada@example.com", 605.50m, "Shipped"),
    new(1002, "alan", "alan@example.com", 420.00m, "Paid"),
];

RouteGroupBuilder group = app.MapGroup("/orders").RequireAuthorization();

group.MapGet("/", (ClaimsPrincipal user) =>
    orders.Where(o => o.CustomerId == user.FindFirstValue("sub")));

group.MapGet("/{id:int}", (int id, ClaimsPrincipal user) =>
    orders.FirstOrDefault(o => o.Id == id && o.CustomerId == user.FindFirstValue("sub")) is { } order
        ? Results.Ok(order)
        : Results.NotFound());

app.Run();
