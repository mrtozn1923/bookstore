using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Bookstore.Catalog;
using Bookstore.Filter;

WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, ApiJsonContext.Default));

WebApplication app = builder.Build();

app.MapGet("/books", (string? filter) =>
{
    if (string.IsNullOrWhiteSpace(filter))
        return Results.Ok(SampleBooks.All);

    CompiledFilter compiled = FilterCompiler.Compile(filter);
    if (!compiled.Success)
        return Results.BadRequest(compiled.Diagnostics.Select(d => d.ToString()).ToArray());

    Func<Book, bool> predicate = compiled.Expression!.Compile();
    return Results.Ok(SampleBooks.All.Where(predicate).ToArray());
});

app.MapGet("/runtime", () => new RuntimeInfo(
    RuntimeFeature.IsDynamicCodeSupported,
    RuntimeFeature.IsDynamicCodeCompiled,
    System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription));

app.Run();

public sealed record RuntimeInfo(bool DynamicCodeSupported, bool DynamicCodeCompiled, string Framework);

[JsonSerializable(typeof(IReadOnlyList<Book>))]
[JsonSerializable(typeof(Book[]))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(RuntimeInfo))]
internal sealed partial class ApiJsonContext : JsonSerializerContext;
