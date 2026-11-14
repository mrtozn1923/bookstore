using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IStringLocalizerFactory, DictionaryStringLocalizerFactory>();
builder.Services.AddLocalization();
builder.Services.AddValidation();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseRequestLocalization("en-US", "tr-TR");

app.MapPost("/books", (CreateBookRequest request) => TypedResults.Ok(request));

app.Run();

public class CreateBookRequest
{
    [Required]
    public string Title { get; set; } = "";

    [Range(0.01, 100000)]
    public decimal Price { get; set; }
}

public class DictionaryStringLocalizerFactory : IStringLocalizerFactory
{
    public IStringLocalizer Create(Type resourceSource) => new DictionaryStringLocalizer();
    public IStringLocalizer Create(string baseName, string location) => new DictionaryStringLocalizer();
}

public class DictionaryStringLocalizer : IStringLocalizer
{
    // Türkçe mesajlar; {0} alan adı, {1} ve {2} Range sınırları
    private static readonly Dictionary<string, string> Turkish = new()
    {
        ["RequiredAttribute_Error"] = "{0} alanı zorunludur.",
        ["RangeAttribute_Error"] = "{0} alanı {1} ile {2} arasında olmalıdır.",
    };

    public LocalizedString this[string name]
    {
        get
        {
            bool found = Thread.CurrentThread.CurrentUICulture.Name == "tr-TR" && Turkish.ContainsKey(name);
            return new LocalizedString(name, found ? Turkish[name] : name, resourceNotFound: !found);
        }
    }

    public LocalizedString this[string name, params object[] arguments] =>
        new(name, string.Format(this[name].Value, arguments), this[name].ResourceNotFound);

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        Turkish.Select(pair => new LocalizedString(pair.Key, pair.Value));
}
