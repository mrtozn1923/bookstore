using System.Collections.Frozen;
using System.Reflection;
using Bookstore.Catalog;

namespace Bookstore.Filter.Binding;

public enum FilterType { Int, Decimal, String, Bool, Error }

public sealed record FieldSymbol(string Name, FilterType Type, PropertyInfo Property);

// The only scope of the language: the fields a filter may read. Anything else is an unknown name.
public sealed class SymbolTable
{
    private readonly FrozenDictionary<string, FieldSymbol> _fields;

    public SymbolTable(IEnumerable<FieldSymbol> fields) =>
        _fields = fields.ToFrozenDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);

    public static SymbolTable ForBooks { get; } = new(
    [
        Field("isbn", FilterType.String, nameof(Book.Isbn)),
        Field("title", FilterType.String, nameof(Book.Title)),
        Field("author", FilterType.String, nameof(Book.Author)),
        Field("price", FilterType.Decimal, nameof(Book.Price)),
        Field("year", FilterType.Int, nameof(Book.Year)),
        Field("stock", FilterType.Int, nameof(Book.Stock)),
    ]);

    public IEnumerable<string> Names => _fields.Keys;

    public FieldSymbol? Lookup(string name) => _fields.GetValueOrDefault(name);

    private static FieldSymbol Field(string name, FilterType type, string property) =>
        new(name, type, typeof(Book).GetProperty(property)!);
}
