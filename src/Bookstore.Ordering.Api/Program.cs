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

string instanceName = builder.Configuration["Ordering:Instance"] ?? "ordering";

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Ordering-Instance"] = instanceName;
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

List<Order> orders =
[
    new(1001, "ada", "ada@example.com", 605.50m, "Shipped"),
    new(1002, "alan", "alan@example.com", 420.00m, "Paid"),
];

List<OrderLine> lines =
[
    new(1001, "9780441172719", 1),
    new(1001, "9780141439587", 1),
    new(1002, "9780441172719", 1),
];

RouteGroupBuilder group = app.MapGroup("/orders").RequireAuthorization();

group.MapGet("/", (ClaimsPrincipal user) =>
    orders.Where(o => o.CustomerId == user.FindFirstValue("sub")));

group.MapGet("/{id:int}", (int id, ClaimsPrincipal user) =>
    orders.FirstOrDefault(o => o.Id == id && o.CustomerId == user.FindFirstValue("sub")) is { } order
        ? Results.Ok(order)
        : Results.NotFound());

// "Have I bought this book before?" is Ordering's rule; the BFF only shows the answer.
group.MapGet("/purchases/{isbn}", (string isbn, ClaimsPrincipal user) =>
{
    int? lastOrderId = orders
        .Where(o => o.CustomerId == user.FindFirstValue("sub"))
        .Join(lines.Where(l => l.Isbn == isbn), o => o.Id, l => l.OrderId, (o, _) => (int?)o.Id)
        .Max();
    return new Purchase(isbn, lastOrderId is not null, lastOrderId);
});

// Institutional orders; the gateway sends some institutions to a dedicated instance.
app.MapGet("/institutions/{institutionId}/orders", (string institutionId, HttpRequest request) => new
{
    InstitutionId = institutionId,
    Instance = instanceName,
    UserIdHeader = request.Headers["X-Bookstore-User-Id"].ToString(),
}).RequireAuthorization();

app.Run();
