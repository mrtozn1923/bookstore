using Microsoft.AspNetCore.Authentication.JwtBearer;
using Bookstore.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Formerly ConfigureServices
builder.Services.AddControllers();
builder.Services.AddSingleton<IBookService, InMemoryBookService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

var app = builder.Build();

// Formerly Configure: UseRouting and UseEndpoints are no longer needed
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
