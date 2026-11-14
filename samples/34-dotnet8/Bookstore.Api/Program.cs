using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Bookstore.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddKeyedSingleton<INotificationService, EmailNotificationService>("email");
builder.Services.AddKeyedSingleton<INotificationService, SmsNotificationService>("sms");

builder.Services.AddSingleton<IBookService, InMemoryBookService>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddDbContext<BookstoreContext>(options => options.UseInMemoryDatabase("Bookstore"));
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddEntityFrameworkStores<BookstoreContext>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<FlashSaleService>();

var app = builder.Build();

app.UseExceptionHandler();

app.MapShortCircuit(404, "robots.txt", "favicon.ico");
app.MapGet("/health/live", () => "Healthy").ShortCircuit();

// Minimal API: the key goes on the parameter
app.MapPost("/api/orders/{id:int}/ship", (int id, [FromKeyedServices("sms")] INotificationService notifier) =>
    TypedResults.Ok(notifier.Send(id)));

app.MapGet("/api/books/{id:int}", (int id, IBookService service) => TypedResults.Ok(service.GetById(id)));
app.MapGet("/boom", () => { throw new InvalidOperationException("Something went wrong."); });

app.MapGroup("/account").MapIdentityApi<IdentityUser>();

app.MapGet("/api/me", (ClaimsPrincipal user) => TypedResults.Ok(user.Identity!.Name))
    .RequireAuthorization();

app.MapGet("/api/flash-sale", (FlashSaleService sale) => TypedResults.Ok(new { Active = sale.IsActive() }));

app.MapControllers();

app.Run();
