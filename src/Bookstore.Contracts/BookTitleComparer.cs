using System.Globalization;

namespace Bookstore.Contracts;

/// <summary>Sorts book titles so that "Volume 2" comes before "Volume 10".</summary>
public static class BookTitleComparer
{
    /// <summary>A case-insensitive comparer that compares digit runs as numbers.</summary>
    public static IComparer<string> Instance { get; } = Create();

    private static IComparer<string> Create()
    {
#if NET10_0_OR_GREATER
        return StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering | CompareOptions.IgnoreCase);
#else
        return NaturalTitleComparer.Instance;
#endif
    }
}
