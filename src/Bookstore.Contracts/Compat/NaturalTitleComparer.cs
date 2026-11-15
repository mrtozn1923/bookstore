namespace Bookstore.Contracts;

// Compiled only for frameworks without CompareOptions.NumericOrdering (see the csproj).
internal sealed class NaturalTitleComparer : IComparer<string>
{
    public static NaturalTitleComparer Instance { get; } = new();

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null || y is null)
        {
            return x is null ? -1 : 1;
        }

        int i = 0;
        int j = 0;
        while (i < x.Length && j < y.Length)
        {
            if (char.IsAsciiDigit(x[i]) && char.IsAsciiDigit(y[j]))
            {
                ReadOnlySpan<char> numberX = ReadNumber(x, ref i);
                ReadOnlySpan<char> numberY = ReadNumber(y, ref j);
                int byNumber = numberX.Length != numberY.Length
                    ? numberX.Length.CompareTo(numberY.Length)
                    : numberX.SequenceCompareTo(numberY);
                if (byNumber != 0)
                {
                    return byNumber;
                }

                continue;
            }

            int byChar = char.ToUpperInvariant(x[i]).CompareTo(char.ToUpperInvariant(y[j]));
            if (byChar != 0)
            {
                return byChar;
            }

            i++;
            j++;
        }

        return (x.Length - i).CompareTo(y.Length - j);
    }

    private static ReadOnlySpan<char> ReadNumber(string text, ref int index)
    {
        int start = index;
        while (index < text.Length && char.IsAsciiDigit(text[index]))
        {
            index++;
        }

        return text.AsSpan(start, index - start).TrimStart('0');
    }
}
