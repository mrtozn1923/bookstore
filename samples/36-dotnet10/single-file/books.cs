#:sdk Microsoft.NET.Sdk.Web
#:property PublishAot=false

var app = WebApplication.Create(args);

app.MapGet("/books", () => new BookDto[] { new(1, "Dune", 450m) });

app.Run("http://localhost:5103");

record BookDto(int Id, string Title, decimal Price);
