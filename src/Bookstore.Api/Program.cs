using System.Globalization;
using System.Text;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Bookstore.Api.Auth;
using Bookstore.Api.Data;
using Bookstore.Api.Filters;
using Bookstore.Api.Middleware;
using Bookstore.Api.Options;
using Bookstore.Api.Security;
using Bookstore.Api.Services;
using Bookstore.Api.Validation;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/bookstore-.log", rollingInterval: RollingInterval.Day));

// Kestrel limit for every request body: 10 MB
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
    // remove the "Server: Kestrel" header from responses
    options.AddServerHeader = false;
});

// limit of the multipart form reader: 10 MB
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
});

builder.Services.AddControllers(options =>
    {
        options.Filters.Add<CatalogVersionHeaderFilter>();
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.ClientErrorMapping[StatusCodes.Status503ServiceUnavailable] = new ClientErrorData
        {
            Title = "Service Unavailable",
            Link = "https://tools.ietf.org/html/rfc7231#section-6.6.4"
        };
    });

builder.Services
    .AddApiVersioning(options =>
    {
        // if no version is given, assume 1.0
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        // report supported and deprecated versions in response headers
        options.ReportApiVersions = true;
        // the version can be read from the URL, the query string or a header
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new QueryStringApiVersionReader("api-version"),
            new HeaderApiVersionReader("X-Api-Version"));

        // only for version 1.0 of the Books API
        options.Policies.Sunset("Books", 1.0)
            .Effective(new DateTimeOffset(2027, 6, 1, 0, 0, 0, TimeSpan.Zero))
            .Link("https://example.com/api/versioning-policy")
                .Title("API versioning policy")
                .Type("text/html");
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        // group names such as "v1", "v2" in Swagger
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderRequestValidator>();
ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("en");
builder.Services.AddDbContext<BookstoreContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Bookstore")));
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<ExecutionTimeFilter>();
builder.Services.AddSingleton<SingletonOperation>();
builder.Services.AddScoped<ScopedOperation>();
builder.Services.AddTransient<TransientOperation>();
builder.Services.AddTransient<OperationReporter>();
builder.Services.AddSingleton<INotifier, EmailNotifier>();
builder.Services.AddSingleton<INotifier, SmsNotifier>();
builder.Services.AddOptions<CatalogOptions>()
    .Bind(builder.Configuration.GetSection(CatalogOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(o => o.Currency.Length == 3, "Currency must be a 3-letter ISO code.")
    .ValidateOnStart();

builder.Services.AddCors(options =>
{
    // we trust only the frontend addresses we know
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("https://bookstore.example.com", "http://localhost:4200")
              .WithMethods("GET", "POST", "PUT", "DELETE")
              .WithHeaders("Content-Type", "Authorization")
              .WithExposedHeaders("X-Catalog-Version")
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10)));
});

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
builder.Services.Configure<JwtOptions>(jwtSection);
var jwt = jwtSection.Get<JwtOptions>()!;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        // the rules we validate an incoming token with
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            // User.Identity.Name and User.IsInRole look at these claims
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanManageBooks", policy =>
        policy.RequireRole("Admin", "StoreStaff"));

    // every requirement in a policy must be met
    options.AddPolicy("ExperiencedStaff", policy =>
        policy.RequireRole("StoreStaff")
              .AddRequirements(new MinimumStaffTenureRequirement(30)));

    options.AddPolicy("CanCancelOrders", policy =>
        policy.RequireRole("Admin", "Customer"));

    options.AddPolicy("OrderOwner", policy =>
        policy.AddRequirements(new OrderOwnerRequirement()));

    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddSingleton<IAuthorizationHandler, MinimumStaffTenureHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, OrderOwnerHandler>();
builder.Services.AddSingleton<OrderStore>();

builder.Services.AddSingleton<UserStore>();
builder.Services.AddSingleton<ITokenService, TokenService>();

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSecurityHeaders();
// CORS comes before authentication: a preflight request carries no token
app.UseCors("Frontend");

// order matters: first find out who you are, then check what you may do
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/api/orders/{orderId:guid}", (Guid orderId) => new { orderId, Status = "Pending" });

app.MapGet("/api/config/page-size", (IConfiguration config) => config.GetValue<int>("Catalog:PageSize"));

app.MapGet("/api/minimal/reporter", (OperationReporter reporter) => reporter.Report());

app.MapGet("/debug/routes", (IEnumerable<EndpointDataSource> sources) =>
    sources.SelectMany(s => s.Endpoints)
           .OfType<RouteEndpoint>()
           .Select(e => e.RoutePattern.RawText));

app.Run();
