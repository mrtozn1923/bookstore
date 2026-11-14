using System.Text.Json.Serialization;
using Bookstore.Api;
using Bookstore.Api.Demo;
using Bookstore.Institutional.Infrastructure;
using Bookstore.Tools;
using Microsoft.EntityFrameworkCore;

// Koddan üretilen diyagram: dotnet run --project src/Bookstore.Api --no-launch-profile -- --er
if (args is ["--er"])
{
    DbContextOptions<BookstoreContext> options = new DbContextOptionsBuilder<BookstoreContext>().UseSqlServer().Options;
    using BookstoreContext model = new(options);
    Console.Write(MermaidErWriter.Write(model.Model));
    return;
}

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
string connectionString = builder.Configuration.GetConnectionString("Bookstore")
    ?? "Server=localhost;Database=BookstoreDb;Trusted_Connection=True;TrustServerCertificate=True";
builder.Services.AddInstitutionalOrdering(connectionString);
if (args.Contains("--demo"))
    builder.Services.AddDemoServices();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<OrderExceptionHandler>();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
WebApplication app = builder.Build();

if (args.Contains("--script"))
{
    using IServiceScope scope = app.Services.CreateScope();
    Console.WriteLine(scope.ServiceProvider.GetRequiredService<BookstoreContext>().Database.GenerateCreateScript());
    return;
}

// --demo: örnek veriyi sıfırdan kurar; --keep-data ile mevcut veriyi koruyup yalnız migration'ları uygular
if (args.Contains("--demo") && !args.Contains("--keep-data"))
    await DemoData.ResetAsync(app.Services);
else
{
    using IServiceScope scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<BookstoreContext>().Database.MigrateAsync();
}

app.UseExceptionHandler();
app.MapOrderEndpoints();
app.Run();
