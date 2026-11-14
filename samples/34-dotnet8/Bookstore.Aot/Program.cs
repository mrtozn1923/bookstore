using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();

Book[] books =
[
    new(1, "Dune", 450m),
    new(2, "Emma", 280m)
];

var api = app.MapGroup("/api/books");
api.MapGet("/", () => books);
api.MapGet("/categories", () => new Category(1, "Science Fiction"));

app.Run();

public record Book(int Id, string Title, decimal Price);

public record Category(int Id, string Name);

// No reflection under AOT: every type that goes into JSON must be listed here
[JsonSerializable(typeof(Book[]))]
[JsonSerializable(typeof(Category))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
